
# Setup

1. One ASG with c6i.xlarge (replace .tmp/sequencer.pem with yours in order to connect)
2. One RDS Aurora Serverless v2 PostgreSQL database cluster and instance
3. One ElastiCache Serverless Redis 7.1 with 1,000 ECPUs per second and Maximum data storage of 1 GB

```
mkdir -p .logs
sh Scripts/create-vpc.sh > ".logs/create-vpc_$(date +"%Y%m%d%H%M%S").log" 2>&1
sh Scripts/create-rds.sh > ".logs/create-rds_$(date +"%Y%m%d%H%M%S").log" 2>&1
sh Scripts/create-redis.sh > ".logs/create-redis_$(date +"%Y%m%d%H%M%S").log" 2>&1
sh Scripts/create-asg.sh > ".logs/create-asg_$(date +"%Y%m%d%H%M%S").log" 2>&1
sh Scripts/wait-for-creation.sh
sh Scripts/generate-prod-config-file.sh
sh Scripts/installServer.sh
```
First script takes approx 2 minutes because of the NAT Gateway and the others only take a few seconds although Aurora database and Redis cache will take minutes to be ready.


## VPC resources (tag names)

- sequencer-key-pair
- sequencer-secret-key-pair
- sequencer-vpc
- sequencer-subnet-public1
- sequencer-subnet-public2
- sequencer-subnet-public3
- sequencer-subnet-private1-ec2
- sequencer-subnet-private2-ec2
- sequencer-subnet-private3-ec2
- sequencer-subnet-private1-rds
- sequencer-subnet-private2-rds
- sequencer-subnet-private3-rds
- sequencer-subnet-group-rds
- sequencer-igw
- sequencer-eip
- sequencer-natgw
- sequencer-rtb-public
- sequencer-rtb-private-ec2
- sequencer-rtb-private-rds
- sequencer-sg-default
- sequencer-sg-public-ec2
- sequencer-sg-private-redis
- sequencer-sg-private-rds

## RDS resources (tag names)

- sequencer-secret-aurora-username
- sequencer-secret-aurora-password
- sequencer-aurora-cluster
- sequencer-aurora-instance

## Redis resources (tag name)s

- sequencer-secret-redis-default-user-password
- sequencer-secret-redis-username
- sequencer-secret-redis-password
- sequencer-redis-default-user
- sequencer-redis-user
- sequencer-redis-user-group
- sequencer-redis

## ASG resources (tag names)

- sequencer-instance-role (not listed by get-resources)
- sequencer-instance-profile (not listed by get-resources)
- sequencer-launch-template
- sequencer-asg


# Run

Cleanup & build again so .env.production file is copied to app folders
```
sh cleanup.sh
sh buildDotnetServices.sh Release
```
Copy apps from local folders to remote server, connect, then launch
```
sh Scripts/installServer.sh
sh Scripts/copyAppsToServer.sh
ssh -i .tmp/sequencer.pem ubuntu@<Public IPv4 address or DNS> (see first echo from previous script)
sh testAllDotnetOnServer.sh
```

Stats from 201 to 300
```json
{"start":201,"count":100,"stats":{"createdToProcessingStats":{"50p":1,"90p":2,"95p":2,"99p":2,"avg":1.34,"min":0,"max":2},"processingToProcessedStats":{"50p":502,"90p":503,"95p":504,"99p":505.01,"avg":502.22,"min":498,"max":506},"processedToSequencingStats":{"50p":49.5,"90p":90.1,"95p":95.1,"99p":101.02,"avg":50.69,"min":1,"max":103},"sequencingToSavedStats":{"50p":0,"90p":0,"95p":0,"99p":0.01,"avg":0.01,"min":0,"max":1},"savedToSequencedStats":{"50p":0,"90p":0,"95p":0,"99p":0.01,"avg":0.01,"min":0,"max":1},"processingToSequencedStats":{"50p":552,"90p":593,"95p":598.1,"99p":603.02,"avg":552.93,"min":504,"max":605},"createdToSequencedStats":{"50p":553.5,"90p":594.1,"95p":600.05,"99p":605.02,"avg":554.27,"min":505,"max":607},"maxCreatedToProcessingSeq":{"max":2,"seq":201},"maxProcessingToProcessedSeq":{"max":506,"seq":225},"maxProcessedToSequencingSeq":{"max":103,"seq":227},"maxSequencingToSavedSeq":{"max":1,"seq":244},"maxSavedToSequencedSeq":{"max":1,"seq":298},"maxCreatedToSequencedSeq":{"max":607,"seq":227}},"check":{"firstSeq":201,"lastSeq":300,"isOrdered":true,"brokenAfter":null,"brokenSeq":null,"ordered":[201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255,256,257,258,259,260,261,262,263,264,265,266,267,268,269,270,271,272,273,274,275,276,277,278,279,280,281,282,283,284,285,286,287,288,289,290,291,292,293,294,295,296,297,298,299,300],"others":[]}}
```

# Delete

Scripts take between 15 to 30 minutes to delete resources like Aurora database and Redis cache.
```
sh Scripts/delete-asg.sh > ".logs/delete-asg_$(date +"%Y%m%d%H%M%S").log" 2>&1
sh Scripts/delete-redis.sh > ".logs/delete-redis_$(date +"%Y%m%d%H%M%S").log" 2>&1
sh Scripts/delete-rds.sh > ".logs/delete-rds_$(date +"%Y%m%d%H%M%S").log" 2>&1
sh Scripts/delete-vpc.sh > ".logs/delete-vpc_$(date +"%Y%m%d%H%M%S").log" 2>&1
```

You may check everything has been deleted with
```
sh Scripts/check-resources.sh
```
Note: NAT Gateway is marked as Deleted by will remain in the list for a while
