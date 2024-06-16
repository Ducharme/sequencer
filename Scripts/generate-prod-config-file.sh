#!/bin/sh

SRC_FILE=.env.example.production
DST_FILE=.env.production

cp $SRC_FILE $DST_FILE

PGSQL_ENDPOINT=$(aws rds describe-db-cluster-endpoints --db-cluster-identifier sequencer-aurora-cluster --filters "Name=db-cluster-endpoint-type,Values=WRITER" --query 'DBClusterEndpoints[0].Endpoint' --output text)
PGSQL_USERNAME=$(aws secretsmanager get-secret-value --secret-id sequencer-aurora-username --query 'SecretString' --output text)
PGSQL_PASSWORD=$(aws secretsmanager get-secret-value --secret-id sequencer-aurora-password --query 'SecretString' --output text)

REDIS_ENDPOINT=$(aws elasticache describe-serverless-caches --serverless-cache-name sequencer-redis --query 'ServerlessCaches[0].Endpoint.Address' --output text)
REDIS_USER=$(aws secretsmanager get-secret-value --secret-id sequencer-redis-username --query 'SecretString' --output text)
REDIS_PASSWORD=$(aws secretsmanager get-secret-value --secret-id sequencer-redis-password --query 'SecretString' --output text)

sed -i 's/PGSQL_ENDPOINT=sequencer.abcd1234.region.rds.amazonaws.com/PGSQL_ENDPOINT='$PGSQL_ENDPOINT'/' .env.production
sed -i 's/PGSQL_USERNAME=myuser/PGSQL_USERNAME='$PGSQL_USERNAME'/' .env.production
sed -i 's/PGSQL_PASSWORD=mypassword/PGSQL_PASSWORD='$PGSQL_PASSWORD'/' .env.production

sed -i 's/REDIS_ENDPOINT=sequencer-abcd1234.serverless.region.cache.amazonaws.com/REDIS_ENDPOINT='$REDIS_ENDPOINT'/' .env.production
sed -i 's/REDIS_USER=myuser/REDIS_USER='$REDIS_USER'/' .env.production
sed -i 's/REDIS_PASSWORD=mypassword/REDIS_PASSWORD='$REDIS_PASSWORD'/' .env.production

echo "DONE!"
