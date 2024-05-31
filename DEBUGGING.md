# Debugging

## Running services

Set group name
```
export GROUP_NAME=poc
```
When inside the project folder
```
dotnet run .env.local
```
When inside the program folder
```
dotnet AdminService.dll .env.local
```
When inside the solution folder
```
dotnet run --project Services/AdminService .env.local
```

## Setup database

```
psql -h sequencer.abcd1234.region.rds.amazonaws.com -U <username> -d sequencer -p 5432
psql -h localhost -U myuser -d sequencer -p 5432 # <password>

\dt
SELECT * FROM events;

CREATE TABLE events (
    id SERIAL PRIMARY KEY,
    name VARCHAR(20) NOT NULL,
    sequence INTEGER NOT NULL,
    payload VARCHAR(2048) NOT NULL,
    delay INTEGER NOT NULL,
    createdAt TIMESTAMP without time zone NOT NULL,
    processingAt TIMESTAMP without time zone NOT NULL,
    processedAt TIMESTAMP without time zone NOT NULL,
    sequencingAt TIMESTAMP without time zone NOT NULL,
    savedAt TIMESTAMP without time zone NOT NULL,
    sequencedAt TIMESTAMP without time zone
);
```

## Setup Redis

```
KEYS *
SCAN 0 MATCH * COUNT 100
LRANGE pending-lst-poc 0 100
LRANGE sequenced-lst-poc 0 100
XINFO STREAM processed-str-poc
XINFO STREAM sequenced-str-poc
XRANGE processed-str-poc - +
XRANGE sequenced-str-poc - +
FLUSHDB
```

[Redis keyspace-notifications](https://redis.io/docs/manual/keyspace-notifications/)
```
K     Keyspace events, published with __keyspace@<db>__ prefix.
E     Keyevent events, published with __keyevent@<db>__ prefix.
$     String commands
s     Set commands
h     Hash commands
x     Expired events (events generated every time a key expires)
e     Evicted events (events generated when a key is evicted for maxmemory)
n     New key events (Note: not included in the 'A' class)

CONFIG SET notify-keyspace-events AKE
```

## Cleanup disk space

```
docker system prune --all --force --volumes
```