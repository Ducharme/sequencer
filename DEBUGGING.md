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

Get the endpoint with the first command and replace the value for the host in the second command. Values will also be in .env.production file if generated and also in Secrets Manager.
```
aws rds describe-db-cluster-endpoints --db-cluster-identifier sequencer-aurora-cluster --filters "Name=db-cluster-endpoint-type,Values=WRITER" --query 'DBClusterEndpoints[0].Endpoint' --output text
psql -h sequencer.abcd1234.region.rds.amazonaws.com -U <username> -d sequencer -p 5432
psql -h localhost -U myuser -d sequencer -p 5432 # <password>
```
Once connected
```
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

Get the endpoint with the first command and replace the value for the host in the second command. Values will also be in .env.production file if generated and also in Secrets Manager.
```
aws elasticache describe-serverless-caches --serverless-cache-name sequencer-redis --query 'ServerlessCaches[0].Endpoint.Address' --output text
redis-cli -h sequencer-redis-abcd1234.serverless.reg.cache.amazonaws.com -p 6379 --tls
```
Once connected
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