#!/bin/sh

FOLDER=$(pwd)

build_arg_1=$1 # --progress=plain
build_arg_2=$2 # --no-cache 

echo "Building arguments: $build_arg_1 $build_arg_2"

echo "Building ProcessorService image..."
cd $FOLDER/Services/ProcessorService/
docker build -f ./Dockerfile --pull $build_arg_1 $build_arg_2 -t processorservice ../..

echo "Building SequencerService image..."
cd $FOLDER/Services/SequencerService/
docker build -f ./Dockerfile --pull $build_arg_1 $build_arg_2 -t sequencerservice ../..

echo "Building AdminWebPortal image..."
cd $FOLDER/WebServices/AdminWebPortal/
docker build -f ./Dockerfile --pull $build_arg_1 $build_arg_2 -t adminwebportal ../..

cd "$FOLDER"

echo "DONE BUILDING!"
