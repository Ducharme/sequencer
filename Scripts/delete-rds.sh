#!/bin/sh

# RDS

aws rds delete-db-instance --db-instance-identifier sequencer-aurora-instance --skip-final-snapshot
echo "Waiting for the DB instance sequencer-aurora-instance to be deleted..."
aws rds wait db-instance-deleted --db-instance-identifier sequencer-aurora-instance
echo "DB instance sequencer-aurora-instance deleted successfully"

aws rds delete-db-cluster --db-cluster-identifier sequencer-aurora-cluster --skip-final-snapshot
echo "Waiting for the DB cluster sequencer-aurora-cluster to be deleted..."
aws rds wait db-cluster-deleted --db-cluster-identifier sequencer-aurora-cluster
echo "DB cluster sequencer-aurora-cluster deleted successfully"

aws secretsmanager delete-secret --secret-id sequencer-aurora-username --force-delete-without-recovery
aws secretsmanager delete-secret --secret-id sequencer-aurora-password --force-delete-without-recovery
