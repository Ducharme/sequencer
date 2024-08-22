#!/bin/sh

# docker login

VERSION=0.0.1

docker tag adminwebportal:latest claudeducharme/adminwebportal:$VERSION
docker push claudeducharme/adminwebportal:$VERSION

docker tag processorservice:latest claudeducharme/processorservice:$VERSION
docker push claudeducharme/processorservice:$VERSION

docker tag sequencerservice:latest claudeducharme/sequencerservice:$VERSION
docker push claudeducharme/sequencerservice:$VERSION
