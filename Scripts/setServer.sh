#!/bin/sh

# TODO: Review to handle more than one server

AWS_ASG_EC2_N1=$(aws autoscaling describe-auto-scaling-groups --filters "Name=tag:Name,Values=sequencer-asg" --query "AutoScalingGroups[0].Instances[0].InstanceId" --output text)
#AWS_EC2_PUBLIC_DNS=$(aws ec2 describe-instances --instance-ids $AWS_ASG_EC2_N1 --filters "Name=tag:Name,Values=sequencer-asg" --query "Reservations[0].Instances[0].PublicDnsName")
AWS_EC2_PUBLIC_IP=$(aws ec2 describe-instances --instance-ids $AWS_ASG_EC2_N1 --filters "Name=tag:Name,Values=sequencer-asg" --query "Reservations[0].Instances[0].PublicIpAddress" --output text)

PEM_FILE=.tmp/sequencer.pem
SERVER=ubuntu@$AWS_EC2_PUBLIC_IP
echo "Connecting to server with command: ssh -i $PEM_FILE $SERVER"
