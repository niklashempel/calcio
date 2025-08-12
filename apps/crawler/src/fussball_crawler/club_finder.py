import sys

from bs4 import BeautifulSoup, Tag

from . import api_client
from .logger import get_logger, setup_logging
from .scraper import fetch_all_clubs_for_post_code

logger = get_logger(__name__)


def main(postal_code: str) -> None:
    api_available = api_client.available()
    if not api_available:
        return

    # Use the new function that handles load-more
    text = fetch_all_clubs_for_post_code(postal_code)
    soup = BeautifulSoup(text, "html.parser")

    # Get club list
    club_list = soup.find_all(id="clublist")
    if len(club_list) == 0:
        return

    # Check if first element is a Tag and has ul attribute
    if not club_list or not isinstance(club_list[0], Tag):
        logger.warning(f"Club list element is not a Tag for postal code: {postal_code}")
        return

    ul_element = club_list[0].find("ul")  # type: ignore[union-attr]
    if not ul_element or not isinstance(ul_element, Tag):
        logger.warning(f"UL element not found for postal code: {postal_code}")
        return

    # Get link and name of every club
    for club in ul_element.find_all("li"):  # type: ignore[union-attr]
        if not isinstance(club, Tag):
            continue

        a_element = club.find("a")
        if not a_element or not isinstance(a_element, Tag):
            logger.warning("No anchor element found in club item")
            continue

        href = a_element.get("href")
        if not href or not isinstance(href, str):
            logger.warning("No valid href found in anchor element")
            continue

        external_id = href.split("/")[-1]
        club_name = a_element.text.strip().split("\n")[0] if a_element.text else None
        if not club_name:
            logger.error("Club name is empty for external_id: %s", external_id)
            continue
        api_client.insert_club(external_id, club_name)


if __name__ == "__main__":
    if len(sys.argv) == 1:
        print(f"Usage: python3 {sys.argv[0]} <post_code>")
    else:
        setup_logging()
        logger = get_logger(__name__)
        input_file = sys.argv[1]
        main(input_file)
