#!/bin/sh

. Scripts/setServer.sh

scp -i $PEM_FILE -r Scripts/installer.sh $SERVER:~/
ssh -i $PEM_FILE $SERVER "sh installer.sh > installer.log 2>&1"

echo "DONE!"
