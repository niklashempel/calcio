import os
import requests
from bs4 import BeautifulSoup, Tag
from fontTools.ttLib import TTFont
from datetime import datetime
import locale
from logger import setup_logging, get_logger
import json

# Setup centralized logging
setup_logging()
logger = get_logger(__name__)

def get_matches(table: Tag, obfuscation_id: str):
    """Extract matches from the fussball.de table"""
    matches = []
    rows = table.find_all("tr")
    for i in range(0, len(rows) - 1, 4):
        headline = rows[i + 1].find(
            "span", {"data-obfuscation": obfuscation_id}
        )  # Sonntag, 15.06.2025 | 09:15 Uhr | B-Junioren | Bezirksliga
        match_details = headline.text.split("|")
        if len(match_details) != 4:
            logger.warning(f"Invalid match details: {headline.text}")
            continue

        club_row = rows[i + 3].find_all(
            "td", {"class": "column-club"}
        )  # ['TSV 1860 München', 'FC Bayern München']
        if len(club_row) != 2:
            logger.warning(f"Invalid clubs: {rows[i + 3].text}")
            continue
        url = club_row[0].find("a")["href"]
        home = club_row[0].find("div", {"class": "club-name"})
        away = club_row[1].find("div", {"class": "club-name"})
        if (
            home == None or away == None
        ):  # TODO: need to handle Absetzung here to remove the match
            logger.warning(f"Invalid clubs: {rows[i + 3].text.strip()}")
            info = club_row[0].find("span", {"class": "info-text"})
            if info:
                logger.warning(f"Info: {info.text.strip()}")
            continue
        home = home.text.strip()
        away = away.text.strip()

        date = match_details[0].strip()
        time = match_details[1].strip()
        datetime_obj = parse_date_time(date, time)
        if datetime_obj == None:
            logger.warning(f"Invalid date: {date} {time}")
            continue
        age_group = match_details[2].strip()
        league = match_details[3].strip()

        # Find div that starts with 'Spielstätte:'
        venue = rows[i + 4].find_all("div")
        if len(venue) == 0:
            logger.warning(f"Venue not found: {rows[i + 4].text}{date}{time}{home}{away}{age_group}{league}")
            continue
        if len(venue) == 1:
            venue = venue[0]
        else:
            venue = venue[1]

        venue_split = venue.text.replace("Spielstätte:", "").split("|")
        if len(venue_split) != 3:
            logger.warning(f"Invalid venue: {venue.text}")
            continue

        name = venue_split[0].strip()
        address = venue_split[1].strip()
        city = venue_split[2].strip()
        matches.append(
            {
                "time": datetime_obj,
                "home": home,
                "away": away,
                "age_group": age_group,
                "league": league,
                "address": address + ", " + city,
                "url": url,
            }
        )
    return matches


def find_obfuscation_id(soup: BeautifulSoup):
    """Find the obfuscation ID from the fussball.de page"""
    headline = soup.find("tr", {"class": "row-headline visible-small"})
    if headline is None:
        logger.warning("Headline not found")
        return None
    span = headline.find("span")
    if span is None or not isinstance(span, Tag):
        logger.warning("Span not found or invalid")
        return None
    obfuscation_id = span.get("data-obfuscation")
    if obfuscation_id is None:
        logger.warning("Obfuscation ID not found")
        return None
    logger.warning(f"Obfuscation ID: {obfuscation_id}")
    return str(obfuscation_id)


def de_obfuscate(obfuscation_id: str, r: requests.Response):
    """De-obfuscate the fussball.de page content using the font file"""
    font_file = requests.get(
        "https://www.fussball.de/export.fontface/-/format/woff/id/"
        + obfuscation_id
        + "/type/font"
    )

    with open("font.woff", "wb") as f:
        f.write(font_file.content)

    ch_to_name = {}

    text = r.text
    with TTFont("font.woff") as f:
        for key, value in f.getBestCmap().items():
            text = text.replace("&#x" + str(format(key, "x")).upper() + ";", value)
            ch_to_name["&#x" + str(format(key, "x")).upper() + ";"] = value

    defaults = {
        "zero": "0",
        "one": "1",
        "two": "2",
        "three": "3",
        "four": "4",
        "five": "5",
        "six": "6",
        "seven": "7",
        "eight": "8",
        "nine": "9",
        "comma": ",",
        "period": ".",
        "colon": ":",
        "&nbsp;": " ",
        "&#x0020;": " ",
        "&#x002d;": "|",
    }

    for key, value in defaults.items():
        text = text.replace(key, value)

    if os.path.exists("font.woff"):
        os.remove("font.woff")

    return text


def parse_date_time(date, time):
    """Parse German date and time format from fussball.de"""
    # Versuche, die Lokalisierung auf Deutsch zu setzen
    try:
        locale.setlocale(locale.LC_TIME, "de_DE.UTF-8")
    except locale.Error:
        logger.warning("Locale de_DE.UTF8 not found")
        pass

    date_format = "%A, %d.%m.%Y %H:%M Uhr"

    try:
        parsed_date = datetime.strptime(date + " " + time, date_format)
        return parsed_date
    except ValueError as e:
        logger.warning(f"Error parsing date: {date} {time} - {e}")
        return None


def fetch_club_matches(club_external_id: str, from_date: str, to_date: str):
    """Fetch matches for a specific club from fussball.de"""
    url = (
        "https://www.fussball.de/vereinsspielplan.druck/-/datum-bis/"
        + to_date
        + "/datum-von/"
        + from_date
        + "/id/"
        + club_external_id
        + "/match-type/-1/max/999/mode/PRINT/show-venues/true#!/"
    )
    
    try:
        r = requests.get(url)
        soup = BeautifulSoup(r.text, "html.parser")
        obfuscation_id = find_obfuscation_id(soup)
        
        if obfuscation_id == None:
            logger.warning(f"No obfuscation ID found for club {club_external_id}")
            return []
        
        html_content = de_obfuscate(obfuscation_id, r)
        soup = BeautifulSoup(html_content, "html.parser")
        table = soup.find("table", {"class": "table table-striped table-full-width"})
        
        if table is None or not isinstance(table, Tag):
            logger.warning(f"No valid table found for club {club_external_id}")
            return []
        
        matches = get_matches(table, obfuscation_id)
        return matches
        
    except Exception as e:
        logger.error(f"Error fetching matches for club {club_external_id}: {e}")
        return []

def fetch_all_clubs_for_post_code(postal_code: str) -> str:
    """Fetch all clubs for a postal code, including load-more results."""
    # Get the initial page
    url = "https://www.fussball.de/suche.verein/-/plz/" + postal_code + "#!/"
    logger.debug("Fetching URL: %s", url)
    
    r = requests.get(url)
    initial_html = r.text
    
    # Parse to check if there's a load-more button
    soup = BeautifulSoup(initial_html, "html.parser")
    load_more_form = soup.find("form", {"data-ajax-resource": True})
    if not load_more_form or not isinstance(load_more_form, Tag):
        return initial_html
    
    # Extract AJAX URL and parameters
    ajax_url = load_more_form.get("data-ajax-resource")
    if not ajax_url or not isinstance(ajax_url, str):
        return initial_html
    
    logger.debug("Found load-more for %s, fetching additional results...", postal_code)
    
    # Fetch additional results via AJAX
    all_html = initial_html
    offset = 20
    max_results = 20
    
    while True:
        ajax_request_url = ajax_url.replace(f"/plz/{postal_code}", f"/plz/{postal_code}/offset/{offset}/max/{max_results}")
        
        try:
            ajax_response = requests.get(ajax_request_url, headers={
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            })
            
            if ajax_response.status_code != 200:
                break
                
            json_data = ajax_response.json()
            
            # Check if we got more results
            if 'html' not in json_data or not json_data['html'].strip():
                break
                
            # Append new results to our HTML
            additional_html = json_data['html']
            all_html = all_html.replace('</ul>', additional_html + '</ul>')
            
            offset += max_results
            logger.debug("Loaded %d more results for %s (offset: %d)", max_results, postal_code, offset)
            
        except (requests.RequestException, json.JSONDecodeError, KeyError) as e:
            logger.warning("Failed to load more results for %s: %s", postal_code, e)
            break
    
    return all_html