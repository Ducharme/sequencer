#!/bin/sh

REDIS_NAME=local-redis
REDIS_IMAGE=redis:7.2-bookworm
REDIS_PORT=6379:6379

#docker run --name $REDIS_NAME -d -p $REDIS_PORT -v $(pwd)/redis-config:/data/ $REDIS_IMAGE redis-server /data/redis.conf
#docker run --name $REDIS_NAME -d -p $REDIS_PORT -v $REDIS_IMAGE redis-server --loglevel debug --notify-keyspace-events AKE
echo "docker run --name $REDIS_NAME -d -p $REDIS_PORT $REDIS_IMAGE" && docker run --name $REDIS_NAME -d -p $REDIS_PORT $REDIS_IMAGE

#docker exec -it $REDIS_NAME bash
#docker exec -it $REDIS_NAME redis-cli -h <ip>
