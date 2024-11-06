#!/bin/sh

# Generate ConfigMap YAML
mkdir -p ../.tmp

export RUN_ENV_FILE=../.env.local

. ../setEnvFileValues.sh

export PGSQL_DATABASE="sequencer"

envsubst < postgres-secret.yml > ../.tmp/postgres-secret-processed.yml
kind apply -f ../.tmp/postgres-secret-processed.yml
