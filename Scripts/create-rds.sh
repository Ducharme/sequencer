#!/bin/sh

AZS=$(aws ec2 describe-availability-zones --query 'AvailabilityZones[*].ZoneName' --output text | tr '\t' ' ')

# RDS

PGSQL_USERNAME=$(tr -dc 'a-zA-Z' < /dev/urandom | fold -w 16 | head -n 1)
PGSQL_PASSWORD=$(tr -dc 'a-zA-Z0-9' < /dev/urandom | fold -w 16 | head -n 1)
#PGSQL_PASSWORD=$(tr -dc 'a-zA-Z0-9!()*+,-.:;<>[]_{|}~' < /dev/urandom | fold -w 20 | head -n 1)

aws secretsmanager create-secret --name sequencer-aurora-username --secret-string "$PGSQL_USERNAME" --description "PGSQL_USERNAME" --tags Key=Name,Value=sequencer-secret-aurora-username
aws secretsmanager create-secret --name sequencer-aurora-password --secret-string "$PGSQL_PASSWORD" --description "PGSQL_PASSWORD" --tags Key=Name,Value=sequencer-secret-aurora-password

SG_RDS_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-private-rds" --query 'SecurityGroups[0].GroupId' --output text)
aws rds create-db-cluster --db-cluster-identifier sequencer-aurora-cluster --engine aurora-postgresql --engine-version 16.2 --engine-mode provisioned \
  --serverless-v2-scaling-configuration MinCapacity=0.5,MaxCapacity=2 --enable-http-endpoint --availability-zones $AZS \
  --vpc-security-group-ids $SG_RDS_ID --db-subnet-group-name sequencer-subnet-group-rds --master-username $PGSQL_USERNAME --master-user-password $PGSQL_PASSWORD --database-name sequencer \
  --backup-retention-period 1 --enable-cloudwatch-logs-exports '["postgresql"]' --storage-encrypted \
  --enable-performance-insights --performance-insights-retention-period 7 --no-deletion-protection --tags Key=Name,Value=sequencer-aurora-cluster # --monitoring-interval 1

aws rds create-db-instance --db-cluster-identifier sequencer-aurora-cluster --db-instance-identifier sequencer-aurora-instance \
  --db-instance-class db.serverless --engine aurora-postgresql --tags Key=Name,Value=sequencer-aurora-instance # --db-name sequencer
