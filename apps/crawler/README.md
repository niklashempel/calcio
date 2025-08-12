# Fussball.de Crawler

## Installation

### Development Installation

```bash
# Clone the repository
git clone https://github.com/niklashempel/calcio.git
cd calcio/apps/crawler

# Create virtual environment
python -m venv .venv
source .venv/bin/activate

# Install in development mode
python -m pip install -e .[dev]
```

## Usage

```bash
# Activate the virtual environment if not already done
source .venv/bin/activate
# Run club finder with post codes CSV file
./crawler --help
./crawler find-clubs 80331
```

# Run match finder

```
python -m src.fussball_crawler.match_finder
```

Set log level

```bash
export LOG_LEVEL=DEBUG  # or INFO, WARNING, ERROR, CRITICAL
```

## Development

### Running Tests

```bash
pytest
```

### Code Formatting

```bash
black src/ tests/
```

### Type Checking

```bash
mypy src/
```
