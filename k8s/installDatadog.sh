#!/bin/sh

mkdir -p .tmp

CUR_DIR=$(pwd)
cd .tmp

. ./setupDatadog.sh

cd $CUR_DIR

echo "DONE!"
