# Photon

We use [Photon](https://github.com/komoot/photon) to find coordinates for an address.

## Usage

Download the latest Photon database for Germany (about 11 GB):

```bash
wget -O - https://download1.graphhopper.com/public/experimental/extracts/by-country-code/de/photon-db-de-latest.tar.bz2 | pbzip2 -cd | tar x
```

Then run the Photon server:

```bash
java -jar photon-*.jarphoton_data
```
