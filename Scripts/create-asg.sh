#!/bin/sh

AWS_ACC_ID=$(aws sts get-caller-identity --query "Account" --output text)

aws iam create-role --role-name SequencerInstanceRole --assume-role-policy-document file://./Scripts/assume-role-policy.json --tags "Key=Name,Value=sequencer-instance-role"
echo "Waiting for role SequencerInstanceRole to be created..."
aws iam wait role-exists --role-name SequencerInstanceRole
aws iam attach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonElastiCacheFullAccess
aws iam attach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess
aws iam attach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonSQSFullAccess
aws iam attach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore
aws iam attach-role-policy --role-name SequencerInstanceRole --policy-arn arn:aws:iam::aws:policy/AmazonSSMPatchAssociation

aws iam create-instance-profile --instance-profile-name SequencerInstanceProfile --tags "Key=Name,Value=sequencer-instance-profile"
aws iam add-role-to-instance-profile --instance-profile-name SequencerInstanceProfile --role-name SequencerInstanceRole

SG_PUBLIC_EC2_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-public-ec2" --query 'SecurityGroups[0].GroupId' --output text)
AMI_ID=$(aws ec2 describe-images --owners 099720109477 --filters "Name=name,Values=ubuntu/images/hvm-ssd/ubuntu-jammy-22.04-amd64-server-*" "Name=root-device-type,Values=ebs" "Name=virtualization-type,Values=hvm" --query 'sort_by(Images, &CreationDate)[-1].ImageId' --output text)
aws ec2 create-launch-template --launch-template-name sequencer-services --version-description "1.0" --launch-template-data '{"IamInstanceProfile":{"Arn":"arn:aws:iam::'$AWS_ACC_ID':instance-profile/SequencerInstanceProfile"},"NetworkInterfaces":[{"AssociatePublicIpAddress":true,"DeviceIndex":0,"Groups":["'$SG_PUBLIC_EC2_ID'"]}],"ImageId":"'$AMI_ID'","InstanceType":"m6i.xlarge","KeyName":"sequencer","UserData":""}' --tag-specifications 'ResourceType=launch-template,Tags=[{Key=Name,Value=sequencer-launch-template}]'

LAUNCH_TEMPLATE_ID=$(aws ec2 describe-launch-templates --launch-template-names sequencer-services --query 'LaunchTemplates[0].LaunchTemplateId' --output text)
#ws ec2 describe-launch-template-versions --launch-template-id $LAUNCH_TEMPLATE_ID --filters "Name=is-default-version,Values=true" --query 'LaunchTemplateVersions[0].LaunchTemplateId' --output text

SUBNET_PRIVATE_EC2_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-public?" --query 'Subnets[*].SubnetId' --output text | tr '\t' ',')
aws autoscaling create-auto-scaling-group --auto-scaling-group-name sequencer-asg --launch-template LaunchTemplateId=$LAUNCH_TEMPLATE_ID --min-size 1 --max-size 1 --desired-capacity 1 --vpc-zone-identifier "$SUBNET_PRIVATE_EC2_IDS" --tags "Key=Name,Value=sequencer-asg"
