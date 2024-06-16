#!/bin/sh

VPC_ID=$(aws ec2 describe-vpcs --filters "Name=tag:Name,Values=sequencer-vpc" --query 'Vpcs[0].VpcId' --output text)
EC2_INSTANCE_IDS=$(aws ec2 describe-instances --filters "Name=vpc-id,Values=$VPC_ID" --query 'Reservations[].Instances[].InstanceId' --output text)

if [ -z "$EC2_INSTANCE_IDS" ]; then
  echo "No instances found in VPC $VPC_ID"
else
  echo "Terminating instances in VPC $VPC_ID..."
  aws ec2 terminate-instances --instance-ids $EC2_INSTANCE_IDS
fi

aws autoscaling delete-auto-scaling-group --auto-scaling-group-name sequencer-asg --force-delete

aws ec2 delete-launch-template --launch-template-name sequencer-services

aws iam remove-role-from-instance-profile --instance-profile-name SequencerInstanceProfile --role-name SequencerInstanceRole
aws iam delete-instance-profile --instance-profile-name SequencerInstanceProfile

aws iam detach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonSSMPatchAssociation
aws iam detach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore
aws iam detach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonSQSFullAccess
aws iam detach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess
aws iam detach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonElastiCacheFullAccess
aws iam delete-role --role-name SequencerInstanceRole
