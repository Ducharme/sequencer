#!/bin/sh

docker create --name temp_container1 adminwebportal
docker export temp_container1 | tar -tvf - > adminwebportal-files.txt
docker rm temp_container1
docker rmi -f adminwebportal

docker create --name temp_container2 processorservice
docker export temp_container2 | tar -tvf - > processorservice-files.txt
docker rm temp_container2
docker rmi -f processorservice

docker create --name temp_container3 sequencerservice
docker export temp_container3 | tar -tvf - > sequencerservice-files.txt
docker rm temp_container3
docker rmi -f sequencerservice
