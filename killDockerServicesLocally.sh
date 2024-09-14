#!/bin/sh

stop_and_remove_container() {
    local name=$1
    local container_id=$(docker ps -a | grep "$name" | awk '{print $1}')
    if [ ! -z "$container_id" ]; then
        echo "docker stop $name $container_id"
        docker stop $container_id > /dev/null
        echo "docker rm $name $container_id"
        docker rm $container_id > /dev/null
    fi
}

stop_and_remove_container "adminservice"
stop_and_remove_container "processorservice"
stop_and_remove_container "sequencerservice"
stop_and_remove_container "adminwebportal"
stop_and_remove_container "processorwebservice"
stop_and_remove_container "sequencerwebservice"

