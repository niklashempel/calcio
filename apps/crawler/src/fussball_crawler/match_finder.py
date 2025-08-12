from urllib.parse import quote, urlencode

import requests

from . import api_client
from . import scraper as fussball_scraper
from .logger import get_logger, setup_logging

setup_logging()
logger = get_logger(__name__)

from_date = "2025-07-25"
to_date = "2025-08-06"


def find_lat_long_online(location: str) -> tuple[float, float] | None:
    payload = {"q": location}
    params = urlencode(payload, quote_via=quote)
    r = requests.get("http://localhost:2322/api", params=params)
    data = r.json()
    if len(data["features"]) == 0:
        logger.info("Location not found: " + location)
        return None

    point = data["features"][0]["geometry"]["coordinates"]
    return (point[1], point[0])


def main() -> None:
    api_available = api_client.available()
    if not api_available:
        return

    clubs = api_client.get_clubs()
    if clubs is None:
        logger.info("No clubs found...")
        clubs = []
    else:
        logger.info("Found " + str(len(clubs)) + " clubs...")

    for progress, club in enumerate(clubs):
        logger.info(
            "Progress: "
            + str(progress + 1)
            + "/"
            + str(len(clubs))
            + " (progress: "
            + str((progress + 1) / len(clubs) * 100)
            + "%)"
        )

        matches = fussball_scraper.fetch_club_matches(club[0], from_date, to_date)

        for match in matches:
            venue_id = api_client.find_venue_location(match["address"])
            if venue_id is None:
                coordinates = find_lat_long_online(match["address"])
                api_client.insert_venue(match["address"], coordinates=coordinates)
                venue_id = api_client.get_venue_id_by_address(match["address"])

            api_client.insert_age_group(match["age_group"])
            age_group_id = api_client.get_age_group_id_by_name(match["age_group"])

            api_client.insert_competition(match["league"])
            competition_id = api_client.get_competition_id_by_name(match["league"])

            # Find or create teams using the new external IDs and URLs
            # For home team: use home_club_id if available, otherwise fallback to current club
            home_club_id = match.get("home_club_id", club[0])

            # Check if home club exists, if not, fetch club info from team URL
            if not api_client.get_club_id_by_external_id(home_club_id) and match.get(
                "home_team_url"
            ):
                club_info = fussball_scraper.fetch_club_name_from_team_url(
                    match["home_team_url"]
                )
                if club_info:
                    # Create the club with the info from the team page
                    api_client.insert_club(club_info["club_id"], club_info["club_name"])
                    home_club_id = club_info["club_id"]  # Use the correct club ID

            home_team_id = api_client.find_or_create_team(
                match["home"], home_club_id, match.get("home_team_id")
            )

            # For away team: use away_club_id if available, otherwise fallback to current club
            away_club_id = match.get("away_club_id", club[0])

            # Check if away club exists, if not, fetch club info from team URL
            if not api_client.get_club_id_by_external_id(away_club_id) and match.get(
                "away_team_url"
            ):
                club_info = fussball_scraper.fetch_club_name_from_team_url(
                    match["away_team_url"]
                )
                if club_info:
                    # Create the club with the info from the team page
                    api_client.insert_club(club_info["club_id"], club_info["club_name"])
                    away_club_id = club_info["club_id"]  # Use the correct club ID

            away_team_id = api_client.find_or_create_team(
                match["away"], away_club_id, match.get("away_team_id")
            )

            # Insert match with proper foreign keys
            if all(
                [
                    isinstance(home_team_id, int),
                    isinstance(away_team_id, int),
                    isinstance(venue_id, int),
                    isinstance(age_group_id, int),
                    isinstance(competition_id, int),
                ]
            ):
                # Type assertions since we've verified they're integers above
                home_id: int = home_team_id  # type: ignore
                away_id: int = away_team_id  # type: ignore
                v_id: int = venue_id  # type: ignore
                age_id: int = age_group_id  # type: ignore
                comp_id: int = competition_id  # type: ignore

                api_client.insert_match(
                    match["url"], match["time"], home_id, away_id, v_id, age_id, comp_id
                )
    logger.info("Finished processing all clubs.")


if __name__ == "__main__":
    main()
