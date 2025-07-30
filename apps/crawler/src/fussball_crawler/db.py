import psycopg2
from typing import Optional
from .logger import setup_logging, get_logger

setup_logging()
logger = get_logger(__name__)

def init():
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )
        cursor = connection.cursor()
        
        # Create clubs table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS clubs (
                id SERIAL PRIMARY KEY, 
                external_id VARCHAR UNIQUE,
                name VARCHAR
            )
        """)
        
        # Create venues table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS venues (
                id SERIAL PRIMARY KEY,
                address VARCHAR UNIQUE,
                location GEOMETRY(Point, 4326)
            )
        """)
        
        # Create age_groups table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS age_groups (
                id SERIAL PRIMARY KEY,
                name VARCHAR UNIQUE
            )
        """)
        
        # Create competitions table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS competitions (
                id SERIAL PRIMARY KEY,
                name VARCHAR UNIQUE
            )
        """)
        
        # Create teams table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS teams (
                id SERIAL PRIMARY KEY,
                name VARCHAR,
                club_id INTEGER REFERENCES clubs(id)
            )
        """)
        
        # Create matches table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS matches (
                id SERIAL PRIMARY KEY,
                url VARCHAR,
                time TIMESTAMP,
                home_team_id INTEGER REFERENCES teams(id),
                away_team_id INTEGER REFERENCES teams(id),
                venue_id INTEGER REFERENCES venues(id),
                age_group_id INTEGER REFERENCES age_groups(id),
                competition_id INTEGER REFERENCES competitions(id)
            )
        """)
        
        connection.commit()
        logger.debug("All tables created successfully")
    except (Exception, psycopg2.Error) as error:
        logger.error("Error while creating tables: %s", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def insert_club(external_id: str, name):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute(
            "INSERT INTO clubs (external_id, name) VALUES (%s, %s) ON CONFLICT (external_id) DO NOTHING",
            (external_id, name)
        )

        connection.commit()
        logger.debug("%s - Record inserted successfully into clubs table", external_id)
    except (Exception, psycopg2.Error) as error:
        logger.error("Error inserting record into clubs table: %s", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def insert_venue(address: str, coordinates: Optional[tuple] = None):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        
        if coordinates:
            cursor.execute(
                """INSERT INTO venues (address, location) 
                   VALUES (%s, ST_GeomFromText('POINT(%s %s)', 4326)) 
                   ON CONFLICT (address) DO NOTHING""",
                (address, coordinates[1], coordinates[0])
            )
        else:
            cursor.execute(
                "INSERT INTO venues (address) VALUES (%s) ON CONFLICT (address) DO NOTHING",
                (address,)
            )

        connection.commit()
        logger.debug(f"{address} - Record inserted successfully into venues table")
    except (Exception, psycopg2.Error) as error:
        logger.error("Error inserting record into venues table: %s", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def get_club_id_by_external_id(external_id: str):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute("SELECT id FROM clubs WHERE external_id = %s", (external_id,))

        result = cursor.fetchone()
        return result[0] if result else None
    except (Exception, psycopg2.Error) as error:
        logger.error("Error fetching club ID: %s", error)
        return None
    finally:
        if connection:
            cursor.close()
            connection.close()

def get_age_group_id_by_name(name: str):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute("SELECT id FROM age_groups WHERE name = %s", (name,))

        result = cursor.fetchone()
        return result[0] if result else None
    except (Exception, psycopg2.Error) as error:
        logger.error("Error fetching age group ID: %s", error)
        return None
    finally:
        if connection:
            cursor.close()
            connection.close()

def get_competition_id_by_name(name: str):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute("SELECT id FROM competitions WHERE name = %s", (name,))

        result = cursor.fetchone()
        return result[0] if result else None
    except (Exception, psycopg2.Error) as error:
        logger.error("Error fetching competition ID", error)
        return None
    finally:
        if connection:
            cursor.close()
            connection.close()

def insert_age_group(name: str):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute(
            "INSERT INTO age_groups (name) VALUES (%s) ON CONFLICT (name) DO NOTHING",
            (name,)
        )

        connection.commit()
        logger.debug(f"{name} - Record inserted successfully into age_groups table")
    except (Exception, psycopg2.Error) as error:
        logger.error("Error inserting record into age_groups table", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def insert_competition(name: str):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute(
            "INSERT INTO competitions (name) VALUES (%s) ON CONFLICT (name) DO NOTHING",
            (name,)
        )

        connection.commit()
        logger.debug(f"{name} - Record inserted successfully into competitions table")
    except (Exception, psycopg2.Error) as error:
        logger.error("Error inserting record into competitions table", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def insert_team(name: str, club_id: int):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute(
            "INSERT INTO teams (name, club_id) VALUES (%s, %s)",
            (name, club_id)
        )

        connection.commit()
        logger.debug(f"{name} - Record inserted successfully into teams table")
    except (Exception, psycopg2.Error) as error:
        logger.error("Error inserting record into teams table", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def find_or_create_team(team_name: str, club_external_id: Optional[str] = None):
    """Find existing team or create new one. Returns team_id or None"""
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )
        cursor = connection.cursor()
        
        # First try to find existing team by name
        cursor.execute("SELECT id FROM teams WHERE name = %s", (team_name,))
        result = cursor.fetchone()
        
        if result:
            return result[0]
        
        # If not found and we have a club_external_id, create the team
        if club_external_id:
            club_id = get_club_id_by_external_id(club_external_id)
            if club_id:
                insert_team(team_name, club_id)
                cursor.execute("SELECT id FROM teams WHERE name = %s AND club_id = %s", (team_name, club_id))
                result = cursor.fetchone()
                return result[0] if result else None
        
        # Create team without club association (club_id = NULL)
        cursor.execute("INSERT INTO teams (name) VALUES (%s) RETURNING id", (team_name,))
        result = cursor.fetchone()
        connection.commit()
        return result[0] if result else None
        
    except Exception as error:
        logger.debug(f"Error finding/creating team {team_name}: {error}")
        return None
    finally:
        if connection:
            cursor.close()
            connection.close()

def insert_match(url: str, time, home_team_id: int, away_team_id: int, 
                venue_id: int, age_group_id: int, competition_id: int):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute(
            """INSERT INTO matches (url, time, home_team_id, away_team_id, 
               venue_id, age_group_id, competition_id) 
               VALUES (%s, %s, %s, %s, %s, %s, %s)""",
            (url, time, home_team_id, away_team_id, venue_id, age_group_id, competition_id)
        )

        connection.commit()
        logger.debug(f"Match - Record inserted successfully into matches table")
    except (Exception, psycopg2.Error) as error:
        logger.error("Error inserting record into matches table", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def get_clubs():
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute("SELECT external_id FROM clubs")

        clubs = cursor.fetchall()
        return clubs
    except (Exception, psycopg2.Error) as error:
        logger.error("Error fetching clubs", error)
    finally:
        if connection:
            cursor.close()
            connection.close()

def find_venue_location(address: str):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute("SELECT id FROM venues WHERE address = %s", (address,))

        venue_id = cursor.fetchone()
        return venue_id[0] if venue_id else None
    except (Exception, psycopg2.Error) as error:
        logger.error("Error fetching venue location", error)
        return None
    finally:
        if connection:
            cursor.close()
            connection.close()

def get_venue_id_by_address(address: str):
    try:
        connection = psycopg2.connect(
            user="user", password="password", host="127.0.0.1", database="db"
        )

        cursor = connection.cursor()
        cursor.execute("SELECT id FROM venues WHERE address = %s", (address,))

        result = cursor.fetchone()
        return result[0] if result else None
    except (Exception, psycopg2.Error) as error:
        logger.error("Error fetching venue ID", error)
        return None
    finally:
        if connection:
            cursor.close()
            connection.close()
