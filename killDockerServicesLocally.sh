#!/bin/sh

PS_NAME=processorservice
PS_CONTAINER_ID=$(docker ps -a | grep "$PS_NAME" | awk '{print $1}')
if [ ! -z "$PS_CONTAINER_ID" ]; then echo "docker stop $PS_NAME $PS_CONTAINER_ID" && docker stop $PS_CONTAINER_ID > /dev/null; fi
if [ ! -z "$PS_CONTAINER_ID" ]; then echo "docker rm $PS_NAME $PS_CONTAINER_ID" && docker rm $PS_CONTAINER_ID > /dev/null; fi

SS_NAME=sequencerservice
SS_CONTAINER_ID=$(docker ps -a | grep "$SS_NAME" | awk '{print $1}')
if [ ! -z "$SS_CONTAINER_ID" ]; then echo "docker stop $SS_NAME $SS_CONTAINER_ID" && docker stop $SS_CONTAINER_ID > /dev/null; fi
if [ ! -z "$SS_CONTAINER_ID" ]; then echo "docker rm $SS_NAME $SS_CONTAINER_ID" && docker rm $SS_CONTAINER_ID > /dev/null; fi

AP_NAME=adminwebportal
AP_CONTAINER_ID=$(docker ps -a | grep "$AP_NAME" | awk '{print $1}')
if [ ! -z "$AP_CONTAINER_ID" ]; then echo "docker stop $AP_NAME $AP_CONTAINER_ID" && docker stop $AP_CONTAINER_ID > /dev/null; fi
if [ ! -z "$AP_CONTAINER_ID" ]; then echo "docker rm $AP_NAME $AP_CONTAINER_ID" && docker rm $AP_CONTAINER_ID > /dev/null; fi
