#!/bin/sh

# EC REDIS

aws elasticache delete-serverless-cache --serverless-cache-name sequencer-redis
echo "Waiting for the cache cluster sequencer-redis to be deleted..."
aws elasticache wait cache-cluster-deleted --cache-cluster-id sequencer-redis --show-cache-node-info

aws elasticache delete-user-group --user-group-id sequencer-power-group
aws elasticache delete-user --user-id sequencer-power-user

aws secretsmanager delete-secret --secret-id sequencer-redis-username --force-delete-without-recovery
aws secretsmanager delete-secret --secret-id sequencer-redis-password --force-delete-without-recovery
aws secretsmanager delete-secret --secret-id sequencer-redis-default-user-password --force-delete-without-recovery
