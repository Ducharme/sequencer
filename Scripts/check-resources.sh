#!/bin/sh

aws resourcegroupstaggingapi get-resources --query 'ResourceTagMappingList[?Tags[?Key==`Name` && starts_with(Value, `sequencer`)]].{ResourceARN: ResourceARN, Tags: Tags}' --output json | jq
