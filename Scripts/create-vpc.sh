#!/bin/sh

MY_IP=$(curl -s http://checkip.amazonaws.com)
AWS_REGION=$(aws configure get region)
echo "AWS_REGION: $AWS_REGION"
AZ0=$(aws ec2 describe-availability-zones --query 'AvailabilityZones[0].ZoneName' --output text)
AZ1=$(aws ec2 describe-availability-zones --query 'AvailabilityZones[1].ZoneName' --output text)
AZ2=$(aws ec2 describe-availability-zones --query 'AvailabilityZones[2].ZoneName' --output text)

KEY_PAIR_NAME=$(aws ec2 describe-key-pairs --key-names sequencer --query 'KeyPairs[*].KeyName' --output text 2>/dev/null)
if [ -z "$KEY_PAIR_NAME" ] || [ "$KEY_PAIR_NAME" = "None" ]; then
  aws ec2 create-key-pair --key-name sequencer --tag-specifications 'ResourceType=key-pair,Tags=[{Key=Name,Value=sequencer-key-pair}]' | jq ".KeyMaterial" | tr -d '"' | sed 's/\\n/\n/g' > Scripts/sequencer.pem
  chmod 400 Scripts/sequencer.pem
  KEY_PAIR_VALUE=$(cat Scripts/sequencer.pem)
  aws secretsmanager create-secret --name sequencer-key-pair --secret-string "$KEY_PAIR_VALUE" --description "sequencer pem file" --tags Key=Name,Value=sequencer-secret-key-pair
fi

# VPC

aws ec2 create-vpc --cidr-block 10.0.0.0/16 --tag-specifications "ResourceType=vpc,Tags=[{Key=Name,Value=sequencer-vpc}]"
VPC_ID=$(aws ec2 describe-vpcs --filters "Name=tag:Name,Values=sequencer-vpc" --query 'Vpcs[0].VpcId' --output text)

aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.1.0/24 --availability-zone $(echo $AZ0) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-public1}]'
aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.2.0/24 --availability-zone $(echo $AZ1) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-public2}]'
aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.3.0/24 --availability-zone $(echo $AZ2) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-public3}]'

aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.4.0/24 --availability-zone $(echo $AZ0) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-private1-ec2}]'
aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.5.0/24 --availability-zone $(echo $AZ1) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-private2-ec2}]'
aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.6.0/24 --availability-zone $(echo $AZ2) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-private3-ec2}]'

aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.7.0/24 --availability-zone $(echo $AZ0) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-private1-rds}]'
aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.8.0/24 --availability-zone $(echo $AZ1) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-private2-rds}]'
aws ec2 create-subnet --vpc-id $VPC_ID --cidr-block 10.0.9.0/24 --availability-zone $(echo $AZ2) --tag-specifications 'ResourceType=subnet,Tags=[{Key=Name,Value=sequencer-subnet-private3-rds}]'

SUBNET_PRIVATE_RDS_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-private?-rds" --query 'Subnets[*].SubnetId' --output text | tr '\t' ' ')
aws rds create-db-subnet-group --db-subnet-group-name sequencer-subnet-group-rds --db-subnet-group-description "Subnet group for Aurora Serverless" --subnet-ids $SUBNET_PRIVATE_RDS_IDS --tags Key=Name,Value=sequencer-subnet-group-rds

# IGW

aws ec2 create-internet-gateway --tag-specifications 'ResourceType=internet-gateway,Tags=[{Key=Name,Value=sequencer-igw}]'
IGW_ID=$(aws ec2 describe-internet-gateways --filters "Name=tag:Name,Values=sequencer-igw" --query 'InternetGateways[0].InternetGatewayId' --output text)
aws ec2 attach-internet-gateway --vpc-id $VPC_ID --internet-gateway-id $IGW_ID

# NAT GW

PUBLIC_SUBNET_ID1=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-public1" --query 'Subnets[0].SubnetId' --output text)
aws ec2 allocate-address --domain vpc --tag-specifications 'ResourceType=elastic-ip,Tags=[{Key=Name,Value=sequencer-eip}]'
EC2_EIP=$(aws ec2 describe-addresses --filters "Name=tag:Name,Values=sequencer-eip" --query 'Addresses[0].AllocationId' --output text)
aws ec2 create-nat-gateway --subnet-id $PUBLIC_SUBNET_ID1 --allocation-id $EC2_EIP --tag-specifications 'ResourceType=natgateway,Tags=[{Key=Name,Value=sequencer-natgw}]'

# RTB

## PUBLIC RTB

DEFAULT_ROUTE_TABLE_ID=$(aws ec2 describe-route-tables --filters "Name=vpc-id,Values=$VPC_ID" "Name=association.main,Values=true" --query 'RouteTables[0].RouteTableId' --output text)
aws ec2 create-tags --resources $DEFAULT_ROUTE_TABLE_ID --tags Key=Name,Value=sequencer-rtb-default

aws ec2 create-route-table --vpc-id $VPC_ID --tag-specifications 'ResourceType=route-table,Tags=[{Key=Name,Value=sequencer-rtb-public}]'
PUBLIC_RTB_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=sequencer-rtb-public" "Name=vpc-id,Values=$VPC_ID" --query 'RouteTables[0].RouteTableId' --output text)
RTB_ASS_ID=$(aws ec2 describe-route-tables --filters "Name=vpc-id,Values=$VPC_ID" "Name=association.main,Values=true" --query "RouteTables[0].Associations[0].RouteTableAssociationId" --output text)
aws ec2 replace-route-table-association --association-id $RTB_ASS_ID --route-table-id $PUBLIC_RTB_ID
aws ec2 delete-route-table --route-table-id $DEFAULT_ROUTE_TABLE_ID

aws ec2 create-route --route-table-id $PUBLIC_RTB_ID --destination-cidr-block 0.0.0.0/0 --gateway-id $IGW_ID

PUBLIC_SUBNET_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-public?" --query "Subnets[*].SubnetId" --output text)
for PUBLIC_SUBNET_ID in $PUBLIC_SUBNET_IDS; do
  aws ec2 associate-route-table --subnet-id $PUBLIC_SUBNET_ID --route-table-id $PUBLIC_RTB_ID
done

## PRIVATE RTB

aws ec2 create-route-table --vpc-id $VPC_ID --tag-specifications 'ResourceType=route-table,Tags=[{Key=Name,Value=sequencer-rtb-private-ec2}]'
PRIVATE_RTB_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=sequencer-rtb-private-ec2" --query 'RouteTables[0].RouteTableId' --output text)
NAT_GW_ID=$(aws ec2 describe-nat-gateways --filter "Name=tag:Name,Values=sequencer-natgw" "Name=state,Values=available,pending" --query 'NatGateways[0].NatGatewayId' --output text)
echo "Waiting for NAT Gateway $NAT_GW_ID to be available..."
aws ec2 wait nat-gateway-available --nat-gateway-ids $NAT_GW_ID
aws ec2 create-route --route-table-id $PRIVATE_RTB_ID --destination-cidr-block 0.0.0.0/0 --nat-gateway-id $NAT_GW_ID
PRIVATE_SUBNET_EC2_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-private?-ec2" --query "Subnets[*].SubnetId" --output text)
for PRIVATE_SUBNET_EC2_ID in $PRIVATE_SUBNET_EC2_IDS; do
  aws ec2 associate-route-table --subnet-id $PRIVATE_SUBNET_EC2_ID --route-table-id $PRIVATE_RTB_ID
done

## PRIVATE RTB RDS

aws ec2 create-route-table --vpc-id $VPC_ID --tag-specifications 'ResourceType=route-table,Tags=[{Key=Name,Value=sequencer-rtb-private-rds}]'
PRIVATE_RTB_RDS_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=sequencer-rtb-private-rds" --query 'RouteTables[0].RouteTableId' --output text)
PRIVATE_SUBNET_RDS_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-private?-rds" --query "Subnets[*].SubnetId" --output text)
for PRIVATE_SUBNET_RDS_ID in $PRIVATE_SUBNET_RDS_IDS; do
  aws ec2 associate-route-table --subnet-id $PRIVATE_SUBNET_RDS_ID --route-table-id $PRIVATE_RTB_RDS_ID
done

# SG

aws ec2 create-security-group --group-name "sequencer-sg-public-ec2" --description "sequencer-sg-public-ec2" --vpc-id $VPC_ID --tag-specifications 'ResourceType=security-group,Tags=[{Key=Name,Value=sequencer-sg-public-ec2}]'
aws ec2 create-security-group --group-name "sequencer-sg-private-redis" --description "sequencer-sg-private-redis" --vpc-id $VPC_ID --tag-specifications 'ResourceType=security-group,Tags=[{Key=Name,Value=sequencer-sg-private-redis}]'
aws ec2 create-security-group --group-name "sequencer-sg-private-rds" --description "sequencer-sg-private-rds" --vpc-id $VPC_ID --tag-specifications 'ResourceType=security-group,Tags=[{Key=Name,Value=sequencer-sg-private-rds}]'

SG_PUBLIC_EC2_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-public-ec2" --query 'SecurityGroups[0].GroupId' --output text)
aws ec2 authorize-security-group-ingress --group-id $SG_PUBLIC_EC2_ID --protocol tcp --port 22 --cidr $MY_IP/32
aws ec2 authorize-security-group-ingress --group-id $SG_PUBLIC_EC2_ID --protocol tcp --port 5000 --cidr $MY_IP/32
aws ec2 authorize-security-group-egress --group-id $SG_PUBLIC_EC2_ID --protocol tcp --port 0-65535 --cidr 0.0.0.0/0

SG_EC_REDIS_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-private-redis" --query 'SecurityGroups[0].GroupId' --output text)
aws ec2 authorize-security-group-ingress --group-id $SG_EC_REDIS_ID --protocol tcp --port 6379 --source-group $SG_PUBLIC_EC2_ID
aws ec2 authorize-security-group-egress --group-id $SG_EC_REDIS_ID --protocol tcp --port 0-65535 --cidr 0.0.0.0/0

SG_RDS_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-private-rds" --query 'SecurityGroups[0].GroupId' --output text)
aws ec2 authorize-security-group-ingress --group-id $SG_RDS_ID --protocol tcp --port 5432 --source-group $SG_PUBLIC_EC2_ID

DEFAULT_SG_ID=$(aws ec2 describe-security-groups --filters "Name=group-name,Values=default" "Name=vpc-id,Values=$VPC_ID" --query "SecurityGroups[0].GroupId" --output text)
aws ec2 create-tags --resources $DEFAULT_SG_ID --tags Key=Name,Value=sequencer-sg-default
