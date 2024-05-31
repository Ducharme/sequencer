#!/bin/sh

. Scripts/setServer.sh

LOGS_DIR=/tmp/sequencer-logs
mkdir -p $LOGS_DIR

echo "Copying files locally"
scp -i $PEM_FILE $SERVER:~/AdminWebPortal/app-awp-*.log $LOGS_DIR/
scp -i $PEM_FILE $SERVER:~/ProcessorService/app-ps-*.log $LOGS_DIR/
scp -i $PEM_FILE $SERVER:~/SequencerService/app-ss-*.log $LOGS_DIR/

echo "DONE!"
