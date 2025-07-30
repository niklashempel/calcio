import requests
from bs4 import BeautifulSoup, Tag
from datetime import datetime
import locale

from .deobfuscator import Deobfuscator
from .logger import setup_logging, get_logger
import json

# Setup centralized logging
setup_logging()
logger = get_logger(__name__)


def get_matches(table: Tag):
    """Extract matches from the fussball.de table"""
    matches = []
    rows = table.find_all("tr")
    current_date = None  # Store the current date for time-only entries

    for i in range(0, len(rows) - 1, 4):
        headline_row = rows[i + 1]
        if not isinstance(headline_row, Tag):
            logger.warning(f"Row at index {i + 1} is not a Tag: {type(headline_row)}")
            continue
        headline = headline_row.find(
            "span", {"data-obfuscation": True}
        )  # Fr, 25.07.25 - 19:30 Uhr | Herren | Kreisfreundschaftsspiele OR just 19:45
        if headline is None:
            logger.warning(f"No headline found in row {i + 1}")
            continue
        headline_text = headline.text.strip()

        # Handle different formats
        if " | " in headline_text:
            # Full format with categories: "Fr, 25.07.25 - 19:30 Uhr | Herren | Kreisfreundschaftsspiele"
            main_parts = headline_text.split(" | ")
            if len(main_parts) >= 3:
                datetime_part = main_parts[0].strip()  # "Fr, 25.07.25 - 19:30 Uhr"
                age_group = main_parts[1].strip()
                league = main_parts[2].strip()

                # Split datetime part by dash
                if " - " in datetime_part:
                    date_time_split = datetime_part.split(" - ")
                    date = date_time_split[0].strip()
                    time = (
                        date_time_split[1].strip().replace(" Uhr", "")
                    )  # Remove "Uhr" if present
                else:
                    logger.warning(f"Invalid datetime format: {datetime_part}")
                    continue

                current_date = date  # Store for subsequent time-only entries
            else:
                logger.warning(f"Invalid full format: {headline_text}")
                continue
        elif current_date and not " | " in headline_text:
            # Time-only format: "19:45" - use the stored date
            date = current_date
            time = headline_text.replace(" Uhr", "")  # Remove "Uhr" if present

            # For time-only entries, get age_group and league from competition row
            comp_row = rows[i + 2] if i + 2 < len(rows) else None
            if comp_row and isinstance(comp_row, Tag):
                comp_row_classes = comp_row.get("class")
                if comp_row_classes and "row-competition" in comp_row_classes:
                    comp_text = comp_row.get_text(strip=True)
                    # Extract age group and league from competition text like "Herren | Kreispokal"
                    if "|" in comp_text:
                        comp_parts = comp_text.split("|")
                        if len(comp_parts) >= 2:
                            age_group = comp_parts[0].strip()
                            league = comp_parts[1].strip()
                        else:
                            age_group = "Unknown"
                            league = comp_parts[0].strip()
                    else:
                        age_group = "Unknown"
                        league = comp_text
                else:
                    age_group = "Unknown"
                    league = "Unknown"
            else:
                age_group = "Unknown"
                league = "Unknown"
        else:
            logger.warning(f"Invalid match details format: {headline_text}")
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
        if home == None or away == None:
            if "spielfrei" in rows[i + 3].text:
                logger.debug(f"Club {home}: spielfrei")
                continue
            info = club_row[0].find("span", {"class": "info-text"})
            if info:
                logger.warning(f"Invalid clubs with info: {info.text.strip()}")
                continue
            logger.warning(f"Invalid clubs: {rows[i + 3].text.strip()}")
            continue

        home = home.text.strip()
        away = away.text.strip()

        datetime_obj = parse_date_time(date, time)
        if datetime_obj == None:
            logger.warning(f"Invalid date: {date} {time}")
            continue

        # Find score in club row
        score = rows[i + 3].find("td", {"class": "column-score"})
        if score is None:
            logger.warning(
                f"Score not found for match: {home} vs {away} on {date} at {time}"
            )
            continue
        score_left = score.find("span", {"class": "score-left"})
        score_right = score.find("span", {"class": "score-right"})
        if score_left is None or score_right is None:
            reason = score.find("span", {"class": "info-text"})
            if reason:
                logger.debug(
                    f"Score parts not found for match: {home} vs {away} on {date} at {time} - Reason: {reason.text.strip()}"
                )
            else:
                logger.warning(
                    f"Score parts not found for match: {home} vs {away} on {date} at {time}"
                )
            continue
        # try:
        #     score_left = int(score_left.text.strip())
        #     score_right = int(score_right.text.strip())
        # except ValueError:
        #     logger.warning(f"Invalid score format for match: {home} vs {away} on {date} at {time}")
        #     continue

        # Find div that starts with 'Spielstätte:'
        venue = rows[i + 4].find_all("div")
        if len(venue) == 0:
            logger.warning(
                f"Venue not found: {rows[i + 4].text}{date}{time}{home}{away}{age_group}{league}"
            )
            continue
        venue = venue[-1]
        if "Spielstätte:" not in venue.text:
            if "Schiedsrichter" in venue.text:
                logger.debug(
                    f"No venue present for: {rows[i + 4].text}{date}{time}{home}{away}{age_group}{league}"
                )
                continue
            logger.warning(f"Spielstätte not found: {venue.text}")
            continue

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


def de_obfuscate(r: requests.Response):
    """De-obfuscate all spans with any obfuscation ID using their respective font files"""
    deobfuscator = Deobfuscator()
    return deobfuscator.deobfuscate_html(r.text)


def parse_date_time(date, time):
    """Parse German date and time format from fussball.de"""
    try:
        locale.setlocale(locale.LC_TIME, "de_DE.UTF-8")
    except locale.Error:
        logger.warning("Locale de_DE.UTF8 not found")
        pass

    # Handle different date formats
    try:
        # Try new short format: "Fr, 25.07.25"
        if len(date.split(".")) == 3 and len(date.split(".")[-1]) == 2:
            # Short year format like "Fr, 25.07.25"
            date_format = "%a, %d.%m.%y %H:%M"
            parsed_date = datetime.strptime(date + " " + time, date_format)
            return parsed_date
        else:
            # Try old long format: "Sonntag, 15.06.2025"
            date_format = "%A, %d.%m.%Y %H:%M Uhr"
            parsed_date = datetime.strptime(date + " " + time, date_format)
            return parsed_date
    except ValueError as e:
        # If both fail, try without "Uhr" suffix
        try:
            date_format = "%A, %d.%m.%Y %H:%M"
            parsed_date = datetime.strptime(date + " " + time, date_format)
            return parsed_date
        except ValueError:
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
        html_content = de_obfuscate(r)
        soup = BeautifulSoup(html_content, "html.parser")
        table = soup.find("table", {"class": "table table-striped table-full-width"})

        if table is None or not isinstance(table, Tag):
            if "Kein Spielbetrieb" in soup.text:
                logger.debug(
                    f"No matches found for club {club_external_id} - No Spielbetrieb"
                )
                return []
            logger.warning(f"No valid table found for club {club_external_id}")
            return []

        matches = get_matches(table)
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
        ajax_request_url = ajax_url.replace(
            f"/plz/{postal_code}",
            f"/plz/{postal_code}/offset/{offset}/max/{max_results}",
        )

        try:
            ajax_response = requests.get(
                ajax_request_url,
                headers={
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest",
                },
            )

            if ajax_response.status_code != 200:
                break

            json_data = ajax_response.json()

            # Check if we got more results
            if "html" not in json_data or not json_data["html"].strip():
                break

            # Append new results to our HTML
            additional_html = json_data["html"]
            all_html = all_html.replace("</ul>", additional_html + "</ul>")

            offset += max_results
            logger.debug(
                "Loaded %d more results for %s (offset: %d)",
                max_results,
                postal_code,
                offset,
            )

        except (requests.RequestException, json.JSONDecodeError, KeyError) as e:
            logger.warning("Failed to load more results for %s: %s", postal_code, e)
            break

    return all_html
