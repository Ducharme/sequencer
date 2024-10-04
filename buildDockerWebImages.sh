#!/bin/sh

FOLDER=$(pwd)

build_arg_1=$1 # --progress=plain
build_arg_2=$2 # --no-cache 

echo "Building arguments: $build_arg_1 $build_arg_2"

echo "Building AdminWebPortal image..."
cd $FOLDER/WebServices/AdminWebPortal/
docker build -f ./Dockerfile --pull $build_arg_1 $build_arg_2 -t adminwebportal ../..

echo "Building ProcessorWebService image..."
cd $FOLDER/WebServices/ProcessorWebService/
docker build -f ./Dockerfile --pull $build_arg_1 $build_arg_2 -t processorwebservice ../..

echo "Building SequencerWebService image..."
cd $FOLDER/WebServices/SequencerWebService/
docker build -f ./Dockerfile --pull $build_arg_1 $build_arg_2 -t sequencerwebservice ../..
cd "$FOLDER"

echo "DONE BUILDING!"
