#!/usr/bin/env python3
"""
CLI wrapper for the fussball crawler tools.
Provides a user-friendly command-line interface for finding clubs by postal code.
"""

import argparse
import os
import re
import sys
from datetime import date

from .club_finder import main as find_clubs_main
from .logger import get_logger, setup_logging
from .match_finder import main as find_matches_main


def validate_postal_code(postal_code: str) -> bool:
    """Validate German postal code format (5 digits)."""
    return bool(re.match(r"^\d{5}$", postal_code))


def find_clubs_command(args: argparse.Namespace) -> int:
    """Handle the find-clubs command."""
    logger = get_logger(__name__)

    postal_codes = []

    if args.postal_code:
        postal_codes = [args.postal_code]
    else:
        # Read postal codes from stdin
        try:
            import sys

            for line in sys.stdin:
                postal_code = line.strip()
                if postal_code:  # Skip empty lines
                    postal_codes.append(postal_code)
        except KeyboardInterrupt:
            logger.info("Operation cancelled by user")
            return 130

        if not postal_codes:
            logger.error("No postal codes provided via argument or stdin")
            return 1

    # Process each postal code
    total_processed = 0
    total_errors = 0

    for postal_code in postal_codes:
        # Validate postal code format
        if not validate_postal_code(postal_code):
            logger.error(
                f"Invalid postal code format: {postal_code}. Expected 5 digits (e.g., 01099)"
            )
            total_errors += 1
            continue

        try:
            logger.info(f"Finding clubs for postal code: {postal_code}")
            find_clubs_main(postal_code)
            total_processed += 1
        except KeyboardInterrupt:
            logger.info("Operation cancelled by user")
            return 130
        except Exception as e:
            logger.error(f"Error during club search for {postal_code}: {e}")
            total_errors += 1

    logger.info(
        f"Completed processing {total_processed} postal codes ({total_errors} errors)"
    )
    return 1 if total_errors > 0 else 0


def find_matches_command(args: argparse.Namespace) -> int:
    """Handle the find-matches command."""
    logger = get_logger(__name__)

    # Handle date logic: if only one date is passed or none, set both to today
    today = date.today().strftime("%Y-%m-%d")

    if not args.from_date or not args.to_date:
        # No dates provided, use today for both
        from_date = today
        to_date = today
    else:
        # Both dates provided by user
        from_date = args.from_date
        to_date = args.to_date

    try:
        logger.info(f"Finding matches for all clubs from {from_date} to {to_date}...")
        find_matches_main(from_date=from_date, to_date=to_date)
        logger.info("Match finding completed successfully")
        return 0
    except KeyboardInterrupt:
        logger.info("Operation cancelled by user")
        return 130
    except Exception as e:
        logger.error(f"Error during match finding: {e}")
        return 1


def create_parser() -> argparse.ArgumentParser:
    """Create and configure the argument parser."""
    # Create parent parser for shared arguments
    parent_parser = argparse.ArgumentParser(add_help=False)
    parent_parser.add_argument(
        "--verbose", "-v", action="store_true", help="Enable verbose logging output"
    )
    parent_parser.add_argument(
        "--quiet", "-q", action="store_true", help="Suppress all output except errors"
    )

    # Create main parser
    parser = argparse.ArgumentParser(
        prog="fussball-crawler",
        description="Tools for crawling football data from fussball.de",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        parents=[parent_parser],
        epilog="""
Examples:
  %(prog)s find-clubs 01099               # Find clubs in Dresden area
  %(prog)s find-clubs 10115               # Find clubs in Berlin area
  %(prog)s find-clubs 80331 --verbose     # Find clubs with verbose logging
  %(prog)s find-matches                   # Find matches for all clubs (today's date)
  %(prog)s find-matches --verbose         # Find matches with verbose logging
  %(prog)s find-matches --from-date 2025-08-01 --to-date 2025-08-31  # Custom date range
  cat postcodes.csv | %(prog)s find-clubs # Process multiple postal codes from stdin
        """,
    )

    today = date.today().strftime("%Y-%m-%d")

    # Subcommands
    subparsers = parser.add_subparsers(
        dest="command", help="Available commands", metavar="COMMAND"
    )

    # find-clubs subcommand - inherits from parent parser
    find_clubs_parser = subparsers.add_parser(
        "find-clubs",
        help="Find clubs by postal code",
        description="Search for football clubs in a specific postal code area",
        parents=[parent_parser],
    )
    find_clubs_parser.add_argument(
        "postal_code",
        nargs="?",  # Make postal_code optional
        help="German postal code (5 digits, e.g., 01099 for Dresden). If not provided, reads from stdin.",
    )
    find_clubs_parser.set_defaults(func=find_clubs_command)

    # find-matches subcommand - inherits from parent parser
    find_matches_parser = subparsers.add_parser(
        "find-matches",
        help="Find matches for all clubs",
        description="Search for matches for all clubs in the database",
        parents=[parent_parser],
    )
    find_matches_parser.add_argument(
        "--from-date",
        default=today,
        help="Start date for match search (YYYY-MM-DD format, default: today)",
    )
    find_matches_parser.add_argument(
        "--to-date",
        default=today,
        help="End date for match search (YYYY-MM-DD format, default: today)",
    )
    find_matches_parser.set_defaults(func=find_matches_command)

    return parser


def main() -> int:
    """Main CLI entry point."""
    parser = create_parser()

    # Handle global flags manually before full parsing
    import sys

    global_verbose = "--verbose" in sys.argv or "-v" in sys.argv
    global_quiet = "--quiet" in sys.argv or "-q" in sys.argv

    args = parser.parse_args()

    # Check for flags at both global and subcommand level
    verbose = global_verbose or getattr(args, "verbose", False)
    quiet = global_quiet or getattr(args, "quiet", False)

    if quiet:
        os.environ["LOG_LEVEL"] = "ERROR"
    elif verbose:
        os.environ["LOG_LEVEL"] = "DEBUG"
    else:
        os.environ["LOG_LEVEL"] = "INFO"

    setup_logging()

    # Handle case where no subcommand is provided
    if not hasattr(args, "func"):
        parser.print_help()
        return 1

    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
