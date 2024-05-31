#!/bin/sh

# TODO: Review to handle more than one server

AWS_ASG_EC2_N1=$(aws autoscaling describe-auto-scaling-groups | jq '.AutoScalingGroups[] | select(.AutoScalingGroupName | startswith("seq")) | .Instances[0].InstanceId' | tr -d '"')
AWS_EC2_PUBLIC_DNS=$(aws ec2 describe-instances --instance-ids $AWS_ASG_EC2_N1 | jq '.Reservations[0].Instances[0].PublicDnsName' | tr -d '"')

PEM_FILE=Scripts/sequencer.pem
SERVER=ubuntu@$AWS_EC2_PUBLIC_DNS
echo "Connecting to server with command: ssh -i $PEM_FILE $SERVER"
