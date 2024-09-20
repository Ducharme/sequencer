#!/bin/sh

# docker login

VERSION=0.0.11-dd

docker tag adminwebportal:latest claudeducharme/adminwebportal:$VERSION
docker push claudeducharme/adminwebportal:$VERSION

docker tag processorwebservice:latest claudeducharme/processorwebservice:$VERSION
docker push claudeducharme/processorwebservice:$VERSION

docker tag sequencerwebservice:latest claudeducharme/sequencerwebservice:$VERSION
docker push claudeducharme/sequencerwebservice:$VERSION
