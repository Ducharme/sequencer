#!/bin/sh

# EC REDIS

echo "Waiting for the cache cluster sequencer-redis to be available..."
get_status() {
  aws elasticache describe-serverless-caches --serverless-cache-name sequencer-redis --query 'ServerlessCaches[0].Status' --output text
}

# Loop until the status is "available"
while true; do
  STATUS=$(get_status)
  if [ "$STATUS" = "available" ]; then
    break
  fi
  echo "Current status: $STATUS. Waiting for status to become 'available'..."
  sleep 5 # Wait for 5 seconds before the next check
done
echo "Cache cluster sequencer-redis is now available"


# RDS

echo "Waiting for the DB cluster sequencer-aurora-cluster to be available..."
aws rds wait db-cluster-available --db-cluster-identifier sequencer-aurora-cluster
echo "DB cluster sequencer-aurora-cluster is now available"

echo "Waiting for the DB instance sequencer-aurora-instance to be available..."
aws rds wait db-instance-available --db-instance-identifier sequencer-aurora-instance
echo "DB instance sequencer-aurora-instance is now available"
