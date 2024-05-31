#!/bin/sh

FOLDER=$(pwd)

ADMIN_SERVICE_PROJECT_FOLDER=$FOLDER/WebServices/AdminWebPortal
SEQUENCER_SERVICE_PROJECT_FOLDER=$FOLDER/Services/SequencerService
PROCESSOR_SERVICE_PROJECT_FOLDER=$FOLDER/Services/ProcessorService

DEBUG_FOLDER=$ADMIN_SERVICE_PROJECT_FOLDER/bin/Debug/net8.0
RELEASE_FOLDER=$ADMIN_SERVICE_PROJECT_FOLDER/bin/Release/net8.0

ADMIN_SERVICE_NAME=AdminWebPortal
SEQUENCER_SERVICE_NAME=SequencerService
PROCESSOR_SERVICE_NAME=ProcessorService

ADMIN_ASSEMBLY_FILE=$ADMIN_SERVICE_NAME.dll
SEQUENCER_ASSEMBLY_FILE=$SEQUENCER_SERVICE_NAME.dll
PROCESSOR_ASSEMBLY_FILE=$PROCESSOR_SERVICE_NAME.dll

DEBUG_FOLDER_FILES_CNT=$(ls -A "$DEBUG_FOLDER" | wc -l)
RELEASE_FOLDER_FILES_CNT=$(ls -A "$RELEASE_FOLDER" | wc -l)
if [ "$DEBUG_FOLDER_FILES_CNT" -gt 0 ]; then DEBUG_FOLDER_EXISTS=True; fi
if [ "$RELEASE_FOLDER_FILES_CNT" -gt 0 ]; then RELEASE_FOLDER_EXISTS=True; fi

if [ -z "$CONFIG" ]; then
    if [ "$DEBUG_FOLDER_EXISTS" = "True" ] && [ "$RELEASE_FOLDER_EXISTS" = "True" ]; then
        MRU_FOLDER=$(find $DEBUG_FOLDER $RELEASE_FOLDER -type d -printf '%T@\t%p\n' | sort -rn | head -n 1 | cut -f2-)
        if [ "$MRU_FOLDER" = "$DEBUG_FOLDER" ]; then
            echo "Debug folder has the most recent files so will be used"
            CONFIG="Debug"
        elif [ "$MRU_FOLDER" = "$RELEASE_FOLDER" ]; then
            echo "Release folder has the most recent files so will be used"
            CONFIG="Release"
        else
            echo "Neither Debug nor Release folder found in MRU_FOLDER"
            exit 1
        fi
    elif [ "$DEBUG_FOLDER_EXISTS" = "True" ]; then
        echo "Only Debug folder exists"
        CONFIG=Debug
    elif [ "$RELEASE_FOLDER_EXISTS" = "True" ]; then
        echo "Only Release folder exists"
        CONFIG=Release
    else
        echo "Neither Debug nor Release folder found"
        exit 1
    fi
fi


if [ "$CONFIG" = "Debug" ] && [ "$DEBUG_FOLDER_EXISTS" = "" ]; then
    sh ./buildDebug.sh
elif [ "$CONFIG" = "Release" ] && [ "$RELEASE_FOLDER_EXISTS" = "" ]; then
    sh ./buildRelease.sh
fi
