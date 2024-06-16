#!/bin/sh

# RDS

aws rds delete-db-instance --db-instance-identifier sequencer-aurora-instance --skip-final-snapshot
echo "Waiting for the DB instance to be deleted..."
aws rds wait db-instance-deleted --db-instance-identifier sequencer-aurora-instance
echo "DB instance deleted successfully"

aws rds delete-db-cluster --db-cluster-identifier sequencer-aurora-cluster --skip-final-snapshot
# Wait for the DB cluster to be deleted
echo "Waiting for the DB cluster to be deleted..."
aws rds wait db-cluster-deleted --db-cluster-identifier sequencer-aurora-cluster
echo "DB cluster deleted successfully"

aws secretsmanager delete-secret --secret-id sequencer-aurora-username --force-delete-without-recovery
aws secretsmanager delete-secret --secret-id sequencer-aurora-password --force-delete-without-recovery
