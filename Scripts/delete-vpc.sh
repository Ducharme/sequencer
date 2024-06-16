#!/bin/sh

VPC_ID=$(aws ec2 describe-vpcs --filters "Name=tag:Name,Values=sequencer-vpc" --query 'Vpcs[0].VpcId' --output text)
PUBLIC_RTB_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=sequencer-rtb-public" --query 'RouteTables[0].RouteTableId' --output text)
PRIVATE_RTB_EC2_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=sequencer-rtb-private-ec2" --query 'RouteTables[0].RouteTableId' --output text)
PRIVATE_RTB_RDS_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=sequencer-rtb-private-rds" --query 'RouteTables[0].RouteTableId' --output text)
IGW_ID=$(aws ec2 describe-internet-gateways --filters "Name=tag:Name,Values=sequencer-igw" --query 'InternetGateways[0].InternetGatewayId' --output text)
NAT_GW_ID=$(aws ec2 describe-nat-gateways --filter "Name=tag:Name,Values=sequencer-natgw" --query 'NatGateways[0].NatGatewayId' --output text)
EC2_EIP=$(aws ec2 describe-addresses --filters "Name=tag:Name,Values=sequencer-eip" --query 'Addresses[0].AllocationId' --output text)
SG_PUBLIC_EC2_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-public-ec2" --query 'SecurityGroups[0].GroupId' --output text)
SG_EC_REDIS_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-private-redis" --query 'SecurityGroups[0].GroupId' --output text)
SG_RDS_ID=$(aws ec2 describe-security-groups --filters "Name=tag:Name,Values=sequencer-sg-private-rds" --query 'SecurityGroups[0].GroupId' --output text)
PUBLIC_SUBNET_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-public?" --query "Subnets[*].SubnetId" --output text)
PRIVATE_SUBNET_EC2_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-private?-ec2" --query "Subnets[*].SubnetId"  --output text)
PRIVATE_SUBNET_RDS_IDS=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=sequencer-subnet-private?-rds" --query "Subnets[*].SubnetId"  --output text)
DEFAULT_ROUTE_TABLE_ID=$(aws ec2 describe-route-tables --filters "Name=tag:Name,Values=sequencer-rtb-default" --query "RouteTables[0].RouteTableId" --output text)

# Delete RDS subnet group
aws rds delete-db-subnet-group --db-subnet-group-name sequencer-subnet-group-rds

# Delete security groups
if [ "$SG_RDS_ID" != "None" ]; then aws ec2 delete-security-group --group-id $SG_RDS_ID; fi
if [ "$SG_EC_REDIS_ID" != "None" ]; then aws ec2 delete-security-group --group-id $SG_EC_REDIS_ID; fi
if [ "$SG_PUBLIC_EC2_ID" != "None" ]; then aws ec2 delete-security-group --group-id $SG_PUBLIC_EC2_ID; fi

# Delete NAT Gateway
if [ "$NAT_GW_ID" != "None" ]; then
  aws ec2 delete-nat-gateway --nat-gateway-id $NAT_GW_ID
  echo "Waiting for NAT Gateway $NAT_GW_ID to be deleted..."
  aws ec2 wait nat-gateway-deleted --nat-gateway-ids $NAT_GW_ID
fi

# Release Elastic IP
EIP_ASS_ID=$(aws ec2 describe-addresses --filters "Name=tag:Name,Values=sequencer-eip" --query "Addresses[0].AssociationId" --output text)
#EIP_PIP=$(aws ec2 describe-addresses --filters "Name=tag:Name,Values=sequencer-eip" --query "Addresses[0].PublicIp" --output text)
#if [ ! -z "$EIP_ASS_ID" ] && [ "$EIP_ASS_ID" != "None" ]; then aws ec2 disassociate-address --public-ip $EIP_PIP; fi
if [ ! -z "$EIP_ASS_ID" ] && [ "$EIP_ASS_ID" != "None" ]; then aws ec2 disassociate-address --association-id $EIP_ASS_ID; fi
if [ "$EC2_EIP" != "None" ]; then aws ec2 release-address --allocation-id $EC2_EIP; fi

# Detach and delete Internet Gateway


if [ "$IGW_ID" != "None" ]; then aws ec2 detach-internet-gateway --vpc-id $VPC_ID --internet-gateway-id $IGW_ID; fi
if [ "$IGW_ID" != "None" ]; then aws ec2 delete-internet-gateway --internet-gateway-id $IGW_ID; fi

# Disassociate route tables from subnets
delete_route_table() {
  local route_table_id=$1
  local subnet_filter=$2

  if [ ! -z "$route_table_id" ] && [ "$route_table_id" != "None" ]; then
    local subnet_ids=$(aws ec2 describe-subnets --filters "Name=tag:Name,Values=$subnet_filter" --query 'Subnets[*].SubnetId' --output text)
    for subnet_id in $subnet_ids; do
      local asso_id=$(aws ec2 describe-route-tables --route-table-ids $route_table_id --query 'RouteTables[].Associations[?SubnetId==`'$subnet_id'`].RouteTableAssociationId' --output text)
      if [ ! -z "$asso_id" ]; then
        aws ec2 disassociate-route-table --association-id $asso_id
      fi
    done
    aws ec2 delete-route-table --route-table-id $route_table_id
  fi
}

if [ "$PRIVATE_RTB_EC2_ID" != "None" ]; then aws ec2 delete-route --route-table-id $PRIVATE_RTB_EC2_ID --destination-cidr-block 0.0.0.0/0; fi
#if [ "$PUBLIC_RTB_ID" != "None" ]; then aws ec2 delete-route --route-table-id $PUBLIC_RTB_ID --destination-cidr-block 10.0.0.0/16; fi

# Delete route tables
delete_route_table "$PRIVATE_RTB_RDS_ID" "sequencer-subnet-private?-rds"
delete_route_table "$PRIVATE_RTB_EC2_ID" "sequencer-subnet-private?-ec2"
delete_route_table "$PUBLIC_RTB_ID" "sequencer-subnet-public?"
if [ "$DEFAULT_ROUTE_TABLE_ID" != "None" ]; then aws ec2 delete-route-table --route-table-id $DEFAULT_ROUTE_TABLE_ID; fi

# Delete subnets
delete_subnets() {
  local subnet_ids=$1

  if [ ! -z "$subnet_ids" ]; then
    for subnet_id in $subnet_ids; do
      aws ec2 delete-subnet --subnet-id $subnet_id
    done
  fi
}

delete_subnets "$PRIVATE_SUBNET_RDS_IDS"
delete_subnets "$PRIVATE_SUBNET_EC2_IDS"
delete_subnets "$PUBLIC_SUBNET_IDS"

# Delete VPC
if [ "$VPC_ID" != "None" ]; then aws ec2 delete-vpc --vpc-id $VPC_ID; fi

# Delete key pair
aws ec2 delete-key-pair --key-name sequencer
