#!/bin/sh

. Scripts/setServer.sh

SLN_FOLDER=.
DBG_FOLDER=bin/Debug/net8.0/
REL_FOLDER=bin/Release/net8.0/

TST_FOLDER=$SLN_FOLDER/WebServices/AdminWebPortal/
if [ -d "$TST_FOLDER/$REL_FOLDER" ]; then
  echo "Setting Release as source folders"
  BIN_FOLDER=$REL_FOLDER
elif [ -d "$TST_FOLDER/$DBG_FOLDER" ]; then
  echo "Setting Debug as source folders"
  BIN_FOLDER=$DBG_FOLDER
else
  echo "Debug and Release folders do not exist, exiting"
  exit 1
fi

scp -i $PEM_FILE -r killDotnetServicesLocally.sh $SERVER:~/
echo "Stopping running services"
ssh -i $PEM_FILE $SERVER "sh killDotnetServicesLocally.sh"

echo "Deleting services folders"
ssh -i $PEM_FILE $SERVER "rm -rf ~/ProcessorService/ && rm -rf ~/SequencerService/ && rm -rf ~/AdminWebPortal/"

PRJ_FOLDER=$SLN_FOLDER/Services/ProcessorService/$BIN_FOLDER
echo "Copying files from $PRJ_FOLDER"
scp -q -i $PEM_FILE -r $PRJ_FOLDER $SERVER:~/ProcessorService/

PRJ_FOLDER=$SLN_FOLDER/Services/SequencerService/$BIN_FOLDER
echo "Copying files from $PRJ_FOLDER"
scp -q -i $PEM_FILE -r $PRJ_FOLDER $SERVER:~/SequencerService/

PRJ_FOLDER=$SLN_FOLDER/WebServices/AdminWebPortal/$BIN_FOLDER
echo "Copying files from $PRJ_FOLDER"
scp -q -i $PEM_FILE -r $PRJ_FOLDER $SERVER:~/AdminWebPortal/

echo "Copying scripts from root folder"
scp -i $PEM_FILE -r Scripts/runProcessorService.sh $SERVER:~/
scp -i $PEM_FILE -r Scripts/runSequencerService.sh $SERVER:~/
scp -i $PEM_FILE -r Scripts/runAdminWebPortal.sh $SERVER:~/
scp -i $PEM_FILE -r Scripts/testAllDotnetOnServer.sh $SERVER:~/
scp -i $PEM_FILE -r Scripts/runAllServices.sh $SERVER:~/
scp -i $PEM_FILE -r Scripts/setEnvVars.sh $SERVER:~/

echo "DONE!"
