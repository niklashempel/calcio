FROM eclipse-temurin:21.0.5_11-jre-noble

RUN apt-get update \  
    && apt-get -y install --no-install-recommends \
    pbzip2 \
    wget \
    procps \
    coreutils \
    tree \
    && rm -rf /var/lib/apt/lists/*

# Create app directory
WORKDIR /app

# Create data directory
RUN mkdir -p /app/photon_data

# Copy the Photon JAR file
COPY photon-opensearch-0.7.2.jar /app/photon.jar

# Expose the default Photon port
EXPOSE 2322

# Use the initialization script
CMD ["java", "-jar", "/app/photon.jar"]
