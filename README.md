# Calcio

[![API CI](https://github.com/niklashempel/calcio/actions/workflows/api.yml/badge.svg)](https://github.com/niklashempel/calcio/actions/workflows/api.yml)
[![Web CI](https://github.com/niklashempel/calcio/actions/workflows/web.yml/badge.svg)](https://github.com/niklashempel/calcio/actions/workflows/web.yml)

Calcio (Italian for "football") is a platform that lets you find football matches in your area. It uses data from [fussball.de](https://www.fussball.de) to display information in an interactive map.

## Quickstart

1. Clone the repository

   ```bash
   git clone https://github.com/niklashempel/calcio.git
   cd calcio
   ```

2. [Setup photon](/apps/photon/README.md)

   Run this from the repository root so files land inside `apps/photon`:

   ```bash
   wget -qO- https://download1.graphhopper.com/public/experimental/extracts/by-country-code/de/photon-db-de-latest.tar.bz2 | pbzip2 -cd | tar -C apps/photon -x
   ```

   If `pbzip2` isn't installed, replace `pbzip2 -cd` with `bzip2 -cd`.

3. Build Docker images

   ```bash
   docker compose build
   ```

4. Run the application

   ```bash
   docker compose up
   ```

5. Import clubs and matches

   ```bash
   cd apps/crawler
   python -m venv .venv
   source .venv/bin/activate
   python -m pip install -e .[dev]

   cat data/dresden.csv | ./crawler find-clubs --api-url http://localhost:8080
   ./crawler find-matches --post-codes data/dresden.csv --api-url http://localhost:8080
   ```

6. Open http://localhost:5173

## Development

### Prerequisites

Before you start, ensure you have the following installed:

- [Docker](https://www.docker.com/get-started)
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) Make sure to install the SDK.
- [Node.js](https://nodejs.org/)
- [pnpm](https://pnpm.io/installation)
- [Python 3.9+](https://www.python.org/downloads/)
