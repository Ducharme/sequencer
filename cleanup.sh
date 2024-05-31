#!/bin/sh

dotnet clean

SLN_FOLDER=.
BIN_FOLDER=bin
OBJ_FOLDER=obj

echo "Deleting bin and obj folders"

rm -rf $SLN_FOLDER/SharedTypes/CommonTypes/$BIN_FOLDER
rm -rf $SLN_FOLDER/SharedTypes/CommonTypes/$OBJ_FOLDER

rm -rf $SLN_FOLDER/DataAccessLayers/DatabaseAccessLayer/$BIN_FOLDER
rm -rf $SLN_FOLDER/DataAccessLayers/DatabaseAccessLayer/$OBJ_FOLDER

rm -rf $SLN_FOLDER/DataAccessLayers/RedisAccessLayer/$BIN_FOLDER
rm -rf $SLN_FOLDER/DataAccessLayers/RedisAccessLayer/$OBJ_FOLDER

rm -rf $SLN_FOLDER/Services/AdminService/$BIN_FOLDER
rm -rf $SLN_FOLDER/Services/AdminService/$OBJ_FOLDER

rm -rf $SLN_FOLDER/Services/ProcessorService/$BIN_FOLDER
rm -rf $SLN_FOLDER/Services/ProcessorService/$OBJ_FOLDER

rm -rf $SLN_FOLDER/Services/SequencerService/$BIN_FOLDER
rm -rf $SLN_FOLDER/Services/SequencerService/$OBJ_FOLDER

rm -rf $SLN_FOLDER/WebServices/AdminWebPortal/$BIN_FOLDER
rm -rf $SLN_FOLDER/WebServices/AdminWebPortal/$OBJ_FOLDER

rm -rf $SLN_FOLDER/WebServices/ProcessorWebService/$BIN_FOLDER
rm -rf $SLN_FOLDER/WebServices/ProcessorWebService/$OBJ_FOLDER

rm -rf $SLN_FOLDER/WebServices/SequencerWebService/$BIN_FOLDER
rm -rf $SLN_FOLDER/WebServices/SequencerWebService/$OBJ_FOLDER

echo "DONE CLEANING!"
