#!/bin/sh

PROC_FILE=$(ps -u | grep "sh testAll" | grep -v "grep" | awk '{print $NF}')
echo "PROC_FILE=$PROC_FILE"

if [ -z "$PROC_FILE" ]; then
    echo "There is no testAll* process running, exiting"
    exit 3
fi

GREP_NB_PROCESSORS=$(grep NB_PROCESSORS $PROC_FILE | head -n 1)
NB_PROCESSORS=$(echo "$GREP_NB_PROCESSORS" | cut -d '=' -f2)
echo "NB_PROCESSORS=$NB_PROCESSORS"

GREP_NB_SEQUENCERS=$(grep NB_SEQUENCERS $PROC_FILE | head -n 1)
NB_SEQUENCERS=$(echo "$GREP_NB_SEQUENCERS" | cut -d '=' -f2)
echo "NB_SEQUENCERS=$NB_SEQUENCERS"

GREP_BUFFER_WAIT_TIME_SEC=$(grep BUFFER_WAIT_TIME_SEC $PROC_FILE | head -n 1)
BUFFER_WAIT_TIME_SEC=$(echo "$GREP_BUFFER_WAIT_TIME_SEC" | cut -d '=' -f2)
echo "BUFFER_WAIT_TIME_SEC=$BUFFER_WAIT_TIME_SEC"
