#!/bin/sh

# EC REDIS

aws elasticache delete-serverless-cache --serverless-cache-name sequencer-redis
echo "Waiting for the cache cluster sequencer-redis to be deleted..."
#aws elasticache wait cache-cluster-deleted --cache-cluster-id sequencer-redis --show-cache-node-info

get_status() {
  aws elasticache describe-serverless-caches --serverless-cache-name sequencer-redis --query 'ServerlessCaches[0].Status' --output text 2>/dev/null
}

while true; do
  STATUS=$(get_status)
  if [ "$STATUS" = "deleted" ] || [ "$STATUS" = "None" ] || [ -z "$STATUS" ]; then
    break
  fi
  echo "Current status: $STATUS. Waiting for cluster to be deleted..."
  sleep 20 # Wait for 20 seconds before the next check
done
echo "Cache cluster sequencer-redis is now deleted"


aws elasticache delete-user-group --user-group-id sequencer-power-group
aws elasticache delete-user --user-id sequencer-power-user

aws secretsmanager delete-secret --secret-id sequencer-redis-username --force-delete-without-recovery
aws secretsmanager delete-secret --secret-id sequencer-redis-password --force-delete-without-recovery
aws secretsmanager delete-secret --secret-id sequencer-redis-default-user-password --force-delete-without-recovery
