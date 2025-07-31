from os import path
from bs4 import BeautifulSoup, Tag
from .scraper import fetch_all_clubs_for_post_code
from . import api_client
import sys
from .logger import setup_logging, get_logger

setup_logging()
logger = get_logger(__name__)


def main(in_file: str) -> None:

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

        # Check if first element is a Tag and has ul attribute
        if not l or not isinstance(l[0], Tag):
            logger.warning(f"Club list element is not a Tag for postal code: {postal_code}")
            continue
            
        ul_element = l[0].find("ul")  # type: ignore[union-attr]
        if not ul_element or not isinstance(ul_element, Tag):
            logger.warning(f"UL element not found for postal code: {postal_code}")
            continue

        # Get link and name of every club
        for club in ul_element.find_all("li"):  # type: ignore[union-attr]
            if not isinstance(club, Tag):
                continue
                
            a_element = club.find("a")
            if not a_element or not isinstance(a_element, Tag):
                logger.warning(f"No anchor element found in club item")
                continue
                
            href = a_element.get("href")
            if not href or not isinstance(href, str):
                logger.warning(f"No valid href found in anchor element")
                continue
                
            external_id = href.split("/")[-1]
            club_name = a_element.text.strip().split("\n")[0] if a_element.text else None
            if not club_name:
                logger.error("Club name is empty for external_id: %s", external_id)
                continue
            api_client.insert_club(external_id, club_name)

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
