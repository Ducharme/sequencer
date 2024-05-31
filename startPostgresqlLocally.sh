#!/bin/sh

export RUN_ENV_FILE=.env.local

. ./setEnvFileValues.sh

PGSQL_NAME=local-postgres
PGSQL_IMAGE=postgres:16.1-bookworm
PGSQL_PORT=5432
PGSQL_ENV1="-e POSTGRES_USER=$PGSQL_USERNAME"
PGSQL_ENV2="-e POSTGRES_PASSWORD=$PGSQL_PASSWORD"
PGSQL_ENV3="-e POSTGRES_DB=$PGSQL_DATABASE"
PGSQL_ENVS="$PGSQL_ENV1 $PGSQL_ENV2 $PGSQL_ENV3"

echo "docker run --name $PGSQL_NAME -d $PGSQL_ENVS -p $PGSQL_PORT:$PGSQL_PORT $PGSQL_IMAGE" && docker run --name $PGSQL_NAME -d $PGSQL_ENVS -p $PGSQL_PORT:$PGSQL_PORT $PGSQL_IMAGE

#PGPASSWORD=yourpassword psql -h localhost -U myuser -d sequencer
#psql "postgresql://myuser:mypassword@localhost:5432/sequencer"

