"""
API client to replace direct database operations.
This module provides the same interface as db.py but uses HTTP API calls instead.
"""

import requests
import os
from typing import Optional, List, Tuple, Any
from .logger import setup_logging, get_logger

setup_logging()
logger = get_logger(__name__)

# API Configuration - supports environment variables
API_BASE_URL = os.getenv("CALCIO_API_URL", "http://localhost:5149")
API_ENVIRONMENT = os.getenv("CALCIO_ENV", "development")  # development, testing, production

class ApiClient:
    def __init__(self, base_url: str = API_BASE_URL):
        self.base_url = base_url
        self.environment = API_ENVIRONMENT
        self.session = requests.Session()
        self.session.headers.update({
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        })
        
        # Log which environment we're connecting to
        logger.info(f"API Client initialized for {self.environment} environment: {self.base_url}")

    def _get(self, endpoint: str) -> requests.Response:
        """Make GET request to API endpoint"""
        url = f"{self.base_url}{endpoint}"
        response = self.session.get(url)
        logger.debug(f"GET {url} -> {response.status_code}")
        return response

    def _post(self, endpoint: str, data: dict) -> requests.Response:
        """Make POST request to API endpoint"""
        url = f"{self.base_url}{endpoint}"
        response = self.session.post(url, json=data)
        logger.debug(f"POST {url} -> {response.status_code}")
        return response

    def _put(self, endpoint: str, data: dict) -> requests.Response:
        """Make PUT request to API endpoint"""
        url = f"{self.base_url}{endpoint}"
        response = self.session.put(url, json=data)
        logger.debug(f"PUT {url} -> {response.status_code}")
        return response


# Global API client instance
api_client = ApiClient()


def init() -> None:
    """Initialize API client (replaces database table creation)"""
    try:
        # Test API connection
        response = api_client._get("/")
        if response.status_code == 200:
            logger.debug("API connection successful")
        else:
            logger.error(f"API connection failed: {response.status_code}")
    except Exception as error:
        logger.error(f"Error connecting to API: {error}")


def insert_club(external_id: str, name: str) -> None:
    """Insert club using API (for compatibility)"""
    try:
        response = api_client._post("/api/clubs/find-or-create", {
            "externalId": external_id,
            "name": name
        })
        if response.status_code in [200, 201]:
            logger.debug(f"{external_id} - Club upserted successfully via API")
        else:
            logger.error(f"Error upserting club via API: {response.status_code} - {response.text}")
    except Exception as error:
        logger.error(f"Error upserting club via API: {error}")


def insert_venue(address: str, coordinates: Optional[tuple] = None) -> None:
    """Insert venue using API"""
    try:
        data = {"address": address}
        if coordinates:
            data["latitude"] = coordinates[0]
            data["longitude"] = coordinates[1]

        response = api_client._post("/api/venues/find-or-create", data)
        if response.status_code in [200, 201]:
            logger.debug(f"{address} - Venue upserted successfully via API")
        else:
            logger.error(f"Error upserting venue via API: {response.status_code} - {response.text}")
    except Exception as error:
        logger.error(f"Error upserting venue via API: {error}")


def get_club_id_by_external_id(external_id: str) -> Optional[int]:
    """Get club ID by external ID using API"""
    try:
        response = api_client._get(f"/api/lookup/clubs/{external_id}/id")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching club ID via API: {error}")
        return None


def find_or_create_club(external_id: str, name: str) -> Optional[int]:
    """Find existing club or create new one using API"""
    try:
        response = api_client._post("/api/clubs/find-or-create", {
            "externalId": external_id,
            "name": name
        })
        if response.status_code in [200, 201]:
            club_data = response.json()
            return club_data.get("id")
        return None
    except Exception as error:
        logger.error(f"Error finding/creating club via API: {error}")
        return None


def get_age_group_id_by_name(name: str) -> Optional[int]:
    """Get age group ID by name using API"""
    try:
        response = api_client._get(f"/api/lookup/age-groups/{name}/id")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching age group ID via API: {error}")
        return None


def get_competition_id_by_name(name: str) -> Optional[int]:
    """Get competition ID by name using API"""
    try:
        response = api_client._get(f"/api/lookup/competitions/{name}/id")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching competition ID via API: {error}")
        return None


def insert_age_group(name: str) -> None:
    """Insert age group using API"""
    try:
        response = api_client._post("/api/age-groups/find-or-create", {"name": name})
        if response.status_code in [200, 201]:
            logger.debug(f"{name} - Age group upserted successfully via API")
        else:
            logger.error(f"Error upserting age group via API: {response.status_code} - {response.text}")
    except Exception as error:
        logger.error(f"Error upserting age group via API: {error}")


def insert_competition(name: str) -> None:
    """Insert competition using API"""
    try:
        response = api_client._post("/api/competitions/find-or-create", {"name": name})
        if response.status_code in [200, 201]:
            logger.debug(f"{name} - Competition upserted successfully via API")
        else:
            logger.error(f"Error upserting competition via API: {response.status_code} - {response.text}")
    except Exception as error:
        logger.error(f"Error upserting competition via API: {error}")


def find_or_create_team(team_name: str, club_external_id: str, team_external_id: Optional[str] = None) -> Optional[int]:
    """Find existing team or create new one using API"""
    try:
        response = api_client._post("/api/teams/find-or-create", {
            "name": team_name,
            "clubExternalId": club_external_id,
            "externalId": team_external_id
        })
        if response.status_code in [200, 201]:
            team_data = response.json()
            return team_data.get("id")
        elif response.status_code == 400:
            logger.error(f"Club with external_id {club_external_id} not found")
            return None
        return None
    except Exception as error:
        logger.error(f"Error finding/creating team via API: {error}")
        return None


def insert_match(
    url: str,
    time: Any,
    home_team_id: int,
    away_team_id: int,
    venue_id: int,
    age_group_id: int,
    competition_id: int,
) -> None:
    """Insert match using API"""
    try:
        # Convert time to ISO format if it's not already a string
        time_str = time.isoformat() if hasattr(time, 'isoformat') else str(time)
        
        response = api_client._post("/api/matches", {
            "url": url,
            "time": time_str,
            "homeTeamId": home_team_id,
            "awayTeamId": away_team_id,
            "venueId": venue_id,
            "ageGroupId": age_group_id,
            "competitionId": competition_id
        })
        if response.status_code in [200, 201]:
            logger.debug("Match inserted successfully via API")
        else:
            logger.error(f"Error inserting match via API: {response.status_code} - {response.text}")
    except Exception as error:
        logger.error(f"Error inserting match via API: {error}")


def get_clubs() -> List[Tuple[Any, ...]]:
    """Get all clubs using API"""
    try:
        response = api_client._get("/api/clubs")
        if response.status_code == 200:
            clubs_data = response.json()
            # Return tuples of external_id to match original function signature
            return [(club.get("externalId"),) for club in clubs_data if club.get("externalId")]
        return []
    except Exception as error:
        logger.error(f"Error fetching clubs via API: {error}")
        return []


def find_venue_location(address: str) -> Optional[int]:
    """Find venue ID by address using API"""
    return get_venue_id_by_address(address)


def get_venue_id_by_address(address: str) -> Optional[int]:
    """Get venue ID by address using API"""
    try:
        response = api_client._get(f"/api/lookup/venues/{address}/id")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching venue ID via API: {error}")
        return None
