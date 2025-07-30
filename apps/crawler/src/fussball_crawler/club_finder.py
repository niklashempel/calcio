from os import path
from bs4 import BeautifulSoup
from .scraper import fetch_all_clubs_for_post_code
from . import db
import sys
from .logger import setup_logging, get_logger

setup_logging()
logger = get_logger(__name__)


def main(in_file: str) -> None:

    # Initialize database
    db.init()

    postal_codes = []
    with open(path.relpath(in_file), "r") as f:
        for line in f:
            postal_codes.append(line.strip())

    logger.info("Read %d postal codes...", len(postal_codes))

    counter = 0
    for postal_code in postal_codes:
        counter += 1
        if counter % 100 == 0:
            logger.debug(
                "Progress: %d/%d (%s)", counter, len(postal_codes), postal_code
            )

        # Use the new function that handles load-more
        text = fetch_all_clubs_for_post_code(postal_code)
        soup = BeautifulSoup(text, "html.parser")

        # Get club list
        l = soup.find_all(id="clublist")
        if len(l) == 0:
            continue

        # Get link and name of every club
        for club in l[0].ul.find_all("li"):
            external_id = club.a["href"].split("/")[-1]
            club_name = club.a.text.strip().split("\n")[0] if club.a.text else None
            if not club_name:
                logger.error("Club name is empty for external_id: %s", external_id)
                continue
            db.insert_club(external_id, club_name)

    logger.info("Finished processing %d postal codes", len(postal_codes))


if __name__ == "__main__":
    setup_logging()
    logger = get_logger(__name__)
    if len(sys.argv) == 1:
        logger.error(
            "No input file provided. Usage: python3 club_finder.py <input_file>"
        )
    else:
        input_file = sys.argv[1]
        main(input_file)
