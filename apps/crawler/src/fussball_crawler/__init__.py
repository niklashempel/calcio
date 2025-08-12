"""
Fussball Crawler - Tools for crawling football data from fussball.de
"""

__version__ = "0.1.0"

from . import cli
from . import club_finder
from . import api_client
from . import logger

__all__ = ['cli', 'club_finder', 'api_client', 'logger']
