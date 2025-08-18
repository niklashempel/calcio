"""API client to replace direct database operations using HTTP endpoints."""

from __future__ import annotations

import contextlib
from datetime import datetime
from typing import Any

import requests

try:
    from zoneinfo import ZoneInfo
except ImportError:
    ZoneInfo = None

from .logger import get_logger

logger = get_logger(__name__)


class ApiClient:
    def __init__(self, base_url: str):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.headers.update(
            {"Content-Type": "application/json", "Accept": "application/json"}
        )

        # Log which environment we're connecting to
        logger.debug(f"API Client initialized for environment: {self.base_url}")

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


# Lazily-initialized singleton client (avoid side effects on import)
_api_client: ApiClient | None = None


def get_client(base_url: str) -> ApiClient:
    global _api_client
    if _api_client is None or _api_client.base_url != base_url:
        # Create new client if none exists or if URL is different
        _api_client = ApiClient(base_url)
    return _api_client


def _get_initialized_client() -> ApiClient:
    """Get the initialized client. Raises error if not initialized."""
    global _api_client
    if _api_client is None:
        raise RuntimeError(
            "API client not initialized. Call get_client(base_url) first."
        )
    return _api_client


def available() -> bool:
    """Initialize API client (replaces database table creation)"""
    try:
        # Test API connection
        response = _get_initialized_client()._get("/")
        if response.status_code == 200:
            logger.debug("API connection successful")
            return True
        else:
            logger.error(f"API connection failed: {response.status_code}")
    except Exception as error:
        logger.error(f"Error connecting to API: {error}")
    return False


def insert_club(external_id: str, name: str, post_code: str | None) -> None:
    """Insert club using API (for compatibility)"""
    try:
        response = _get_initialized_client()._post(
            "/api/clubs/find-or-create",
            {"externalId": external_id, "name": name, "postCode": post_code},
        )
        if response.status_code in [200, 201]:
            logger.debug(f"{external_id} - Club upserted successfully via API")
        else:
            logger.error(f"Error upserting club via API: {response.text}")
    except Exception as error:
        logger.error(f"Error upserting club via API: {error}")


def insert_venue(address: str, coordinates: tuple | None = None) -> None:
    """Insert venue using API"""
    try:
        data = {"address": address}
        if coordinates:
            data["latitude"] = coordinates[0]
            data["longitude"] = coordinates[1]

        response = _get_initialized_client()._post("/api/venues/find-or-create", data)
        if response.status_code in [200, 201]:
            logger.debug(f"{address} - Venue upserted successfully via API")
        else:
            logger.error(
                f"Error upserting venue via API: {response.status_code} - {response.text}"
            )
    except Exception as error:
        logger.error(f"Error upserting venue via API: {error}")


def get_club_id_by_external_id(external_id: str) -> int | None:
    """Get club ID by external ID using API"""
    try:
        response = _get_initialized_client()._get(f"/api/clubs/find/{external_id}/id")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching club ID via API: {error}")
        return None


def find_or_create_club(external_id: str, name: str) -> int | None:
    """Find existing club or create new one using API"""
    try:
        response = _get_initialized_client()._post(
            "/api/clubs/find-or-create", {"externalId": external_id, "name": name}
        )
        if response.status_code in [200, 201]:
            club_data = response.json()
            return club_data.get("id")
        return None
    except Exception as error:
        logger.error(f"Error finding/creating club via API: {error}")
        return None


def get_age_group_id_by_name(name: str) -> int | None:
    """Get age group ID by name using API"""
    try:
        response = _get_initialized_client()._get(f"/api/age-groups/find/{name}/id")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching age group ID via API: {error}")
        return None


def get_competition_id_by_name(name: str) -> int | None:
    """Get competition ID by name using API"""
    try:
        response = _get_initialized_client()._get(f"/api/competitions/find/{name}/id")
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching competition ID via API: {error}")
        return None


def insert_age_group(name: str) -> None:
    """Insert age group using API"""
    try:
        response = _get_initialized_client()._post(
            "/api/age-groups/find-or-create", {"name": name}
        )
        if response.status_code in [200, 201]:
            logger.debug(f"{name} - Age group upserted successfully via API")
        else:
            logger.error(
                f"Error upserting age group via API: {response.status_code} - {response.text}"
            )
    except Exception as error:
        logger.error(f"Error upserting age group via API: {error}")


def insert_competition(name: str) -> None:
    """Insert competition using API"""
    try:
        response = _get_initialized_client()._post(
            "/api/competitions/find-or-create", {"name": name}
        )
        if response.status_code in [200, 201]:
            logger.debug(f"{name} - Competition upserted successfully via API")
        else:
            logger.error(
                f"Error upserting competition via API: {response.status_code} - {response.text}"
            )
    except Exception as error:
        logger.error(f"Error upserting competition via API: {error}")


def find_or_create_team(
    team_name: str, club_external_id: str, team_external_id: str | None = None
) -> int | None:
    """Find existing team or create new one using API"""
    try:
        response = _get_initialized_client()._post(
            "/api/teams/find-or-create",
            {
                "name": team_name,
                "clubExternalId": club_external_id,
                "externalId": team_external_id,
            },
        )
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


def upsert_match(
    url: str,
    time: Any,
    home_team_id: int | None,
    away_team_id: int | None,
    venue_id: int,
    age_group_id: int,
    competition_id: int,
) -> None:
    """Upsert match using API"""
    try:
        # Normalize time: if datetime and naive, assume Europe/Berlin and attach tz
        if isinstance(time, datetime):
            dt = time
            if dt.tzinfo is None and ZoneInfo is not None:
                with contextlib.suppress(Exception):
                    dt = dt.replace(tzinfo=ZoneInfo("Europe/Berlin"))
            time_str = dt.isoformat()
        else:
            time_str = str(time)

        response = _get_initialized_client()._post(
            "/api/matches",
            {
                "url": url,
                "time": time_str,
                "homeTeamId": home_team_id,
                "awayTeamId": away_team_id,
                "venueId": venue_id,
                "ageGroupId": age_group_id,
                "competitionId": competition_id,
            },
        )
        if response.status_code in [200, 201]:
            logger.debug("Match inserted successfully via API")
        else:
            logger.error(
                f"Error inserting match via API: {response.status_code} - {response.text}"
            )
    except Exception as error:
        logger.error(f"Error inserting match via API: {error}")


def get_clubs(post_codes: list[str] | None = None) -> list[tuple[Any, ...]]:
    """Get all clubs using API, optionally filtered by postal codes.

    The backend expects repeated PostCodes query parameters, e.g.:
    /api/clubs?PostCodes=12345&PostCodes=23456
    """
    try:
        endpoint = "/api/clubs"
        if post_codes:
            query = "&".join(f"PostCodes={post_code}" for post_code in post_codes)
            endpoint += f"?{query}"
        response = _get_initialized_client()._get(endpoint)
        if response.status_code == 200:
            clubs_data = response.json()
            return [
                (club.get("externalId"),)
                for club in clubs_data
                if club.get("externalId")
            ]
        else:
            logger.error(
                f"Error fetching clubs via API: {response.status_code} - {response.text}"
            )
        return []
    except Exception as error:
        logger.error(f"Error fetching clubs via API: {error}")
        return []


def find_venue_location(address: str) -> int | None:
    """Find venue ID by address using API"""
    return get_venue_id_by_address(address)


def get_venue_id_by_address(address: str) -> int | None:
    """Get venue ID by address using API"""
    try:
        response = _get_initialized_client()._get(
            f"/api/venues/find/by-address/{address}/id"
        )
        if response.status_code == 200:
            return response.json()
        return None
    except Exception as error:
        logger.error(f"Error fetching venue ID via API: {error}")
        return None
