#!/bin/sh

SUBNET_PRIVATE_EC2_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-public?" --query 'Subnets[*].SubnetId' --output text | tr '\t' ' ')
SG_EC_REDIS_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-private-redis" --query 'SecurityGroups[0].GroupId' --output text)

# EC REDIS

REDIS_USER=sequencer-power-user
REDIS_PASSWORD=$(tr -dc 'a-zA-Z0-9' < /dev/urandom | fold -w 16 | head -n 1)
REDIS_DEFAULT_USER=default
REDIS_USER_GROUP=sequencer-power-group

DEFAULT_USER=$(aws elasticache describe-users --user-id $REDIS_DEFAULT_USER --query "Users[0].UserId" --output text 2>/dev/null)
if [ -z "$DEFAULT_USER" ] || [ "$DEFAULT_USER" = "None" ]; then
  REDIS_DEFAULT_USER_PASSWORD=$(tr -dc 'a-zA-Z0-9' < /dev/urandom | fold -w 16 | head -n 1)
  aws secretsmanager create-secret --name sequencer-redis-default-user-password --secret-string "$REDIS_DEFAULT_USER_PASSWORD" --description "$REDIS_DEFAULT_USER" --tags Key=Name,Value=sequencer-secret-redis-default-user-password
  aws elasticache create-user --user-id $REDIS_DEFAULT_USER --user-name $REDIS_DEFAULT_USER --engine Redis --passwords $REDIS_DEFAULT_USER_PASSWORD --access-string "on ~* +@all" --tags "Key=Name,Value=sequencer-redis-default-user"
fi

aws secretsmanager create-secret --name sequencer-redis-username --secret-string "$REDIS_USER" --description "REDIS_USER" --tags Key=Name,Value=sequencer-secret-redis-username
aws secretsmanager create-secret --name sequencer-redis-password --secret-string "$REDIS_PASSWORD" --description "REDIS_PASSWORD" --tags Key=Name,Value=sequencer-secret-redis-password

aws elasticache create-user --user-id $REDIS_USER --user-name $REDIS_USER --engine Redis --passwords $REDIS_PASSWORD --access-string "on ~* +@all" --tags "Key=Name,Value=sequencer-redis-user"
aws elasticache create-user-group --user-group-id $REDIS_USER_GROUP --engine Redis --user-ids $REDIS_DEFAULT_USER $REDIS_USER --tags "Key=Name,Value=sequencer-role" --tags "Key=Name,Value=sequencer-redis-user-group"

RETRY_INTERVAL=5  # Time in seconds to wait between checks
check_user_group_status() {
  aws elasticache describe-user-groups --user-group-id "$USER_GROUP_ID" --query 'UserGroups[0].Status' --output text
}

# Wait until the user group is active
while true; do
  STATUS=$(check_user_group_status)
  echo "Current status of user group $USER_GROUP_ID: $STATUS"
  if [ "$STATUS" = "active" ]; then
    break
  fi
  echo "Waiting for user group $USER_GROUP_ID to become active..."
  sleep $RETRY_INTERVAL
done

aws elasticache create-serverless-cache --serverless-cache-name sequencer-redis --engine redis --major-engine-version 7 \
  --subnet-ids $SUBNET_PRIVATE_EC2_IDS --security-group-ids $SG_EC_REDIS_ID --user-group-id $REDIS_USER_GROUP \
  --cache-usage-limits 'DataStorage={Maximum=1,Unit=GB},ECPUPerSecond={Maximum=1000}' --tags Key=Name,Value=sequencer-redis
