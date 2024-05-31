#!/bin/sh

if [ -n "$RUN_ENV_FILE" ]; then
    echo "RUN_ENV_FILE is set, using ENV_FILE=$RUN_ENV_FILE"
    ENV_FILE=$RUN_ENV_FILE
elif [ -n "$1" ]; then
    echo "Using ENV_FILE=$1"
    ENV_FILE=$1
else
    echo "Environment variable RUN_ENV_FILE and path to the .env file as argument are not set, exiting"
    exit 1
fi

if [ ! -f "$ENV_FILE" ]; then
    echo "The provided .env file does not exist: $ENV_FILE"
    exit 2
fi

GREP_GROUP_NAME=$(grep GROUP_NAME $ENV_FILE)
GREP_PGSQL_ENDPOINT=$(grep PGSQL_ENDPOINT $ENV_FILE)
GREP_PGSQL_USERNAME=$(grep PGSQL_USERNAME $ENV_FILE)
GREP_PGSQL_PASSWORD=$(grep PGSQL_PASSWORD $ENV_FILE)
GREP_PGSQL_DATABASE=$(grep PGSQL_DATABASE $ENV_FILE)

if [ -z "$GREP_GROUP_NAME" ]; then
    echo "The provided .env file does not contain GROUP_NAME variable"
    exit 3
fi

export GROUP_NAME=$(echo "$GREP_GROUP_NAME" | cut -d '=' -f2)
echo "GROUP_NAME=$GROUP_NAME"
export PGSQL_ENDPOINT=$(echo "$GREP_PGSQL_ENDPOINT" | cut -d '=' -f2)
echo "PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
export PGSQL_USERNAME=$(echo "$GREP_PGSQL_USERNAME" | cut -d '=' -f2)
echo "PGSQL_USERNAME=$PGSQL_USERNAME"
export PGSQL_PASSWORD=$(echo "$GREP_PGSQL_PASSWORD" | cut -d '=' -f2)
echo "PGSQL_PASSWORD=*****"
export PGSQL_DATABASE=$(echo "$GREP_PGSQL_DATABASE" | cut -d '=' -f2)
echo "PGSQL_DATABASE=$PGSQL_DATABASE"
