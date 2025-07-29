import db
import fussball_scraper
from urllib.parse import urlencode, quote
import requests
from logger import setup_logging, get_logger

setup_logging()
logger = get_logger(__name__)

DEBUG = False

from_date = "2025-07-25"
to_date = "2025-08-06"

def find_lat_long_online(location: str):
    payload = {"q": location}
    params = urlencode(payload, quote_via=quote)
    r = requests.get("http://localhost:2322/api", params=params)
    data = r.json()
    if len(data["features"]) == 0:
        logger.info("Location not found: " + location)
        return None
    
    point = data["features"][0]["geometry"]["coordinates"]
    return (point[1], point[0])


def main():
    db.init()
    clubs = db.get_clubs()
    if clubs is None:
        logger.info("No clubs found...")
        clubs = []
    else:
        logger.info("Found " + str(len(clubs)) + " clubs...")

    progress = 0

    for club in clubs:
        progress += 1
        if progress % 100 == 0:
            logger.info(
                "Progress: "
                + str(progress)
                + "/"
                + str(len(clubs))
                + " (progress: "
                + str(progress / len(clubs) * 100)
                + "%)"
            )

        matches = fussball_scraper.fetch_club_matches(club[0], from_date, to_date)
        
        for match in matches:
            venue_id = db.find_venue_location(match["address"])
            if venue_id == None:
                coordinates = find_lat_long_online(match["address"])
                db.insert_venue(match["address"], coordinates=coordinates)
                venue_id = db.get_venue_id_by_address(match["address"])
            
            db.insert_age_group(match["age_group"])
            age_group_id = db.get_age_group_id_by_name(match["age_group"])
            
            db.insert_competition(match["league"])
            competition_id = db.get_competition_id_by_name(match["league"])
            
            home_team_id = db.find_or_create_team(match["home"], club[0])
            away_team_id = db.find_or_create_team(match["away"])
            
            # Insert match with proper foreign keys
            if all([isinstance(home_team_id, int), isinstance(away_team_id, int), 
                    isinstance(venue_id, int), isinstance(age_group_id, int), isinstance(competition_id, int)]):
                # Type assertions since we've verified they're integers above
                home_id: int = home_team_id  # type: ignore
                away_id: int = away_team_id  # type: ignore
                v_id: int = venue_id         # type: ignore
                age_id: int = age_group_id   # type: ignore
                comp_id: int = competition_id # type: ignore
                
                db.insert_match(
                    match["url"],
                    match["time"],
                    home_id,
                    away_id,
                    v_id,
                    age_id,
                    comp_id
                )
    logger.info("Finished processing all clubs.")

if __name__ == "__main__":
    main()
    