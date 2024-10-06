#!/bin/sh

# docker login

BASE_VERSION=0.0.2
MAIN_VERSION=0.0.26
REPO_NAME=claudeducharme

push_images() {
    local image_list=$1

    images=$(echo "$image_list" | tr -s '\r\n' ' ')
    echo "$image_list" | while read image; do

        image_name=$(echo "$image" | cut -d':' -f1) # Already prefixed by the repo anme
        image_tag=$(echo "$image" | cut -d':' -f2)
        echo "Checking if already exist on https://hub.docker.com/v2/repositories/$image_name/tags/$image_tag"
        BASE_INFO=$(curl -s "https://hub.docker.com/v2/repositories/$image_name/tags/$image_tag")
        if echo "$BASE_INFO" | grep -q "digest"; then
            echo "Image $image already exists, skipping docker push"
        else
            echo "Pushing image $image"
            docker push $image
        fi
    done
}

BASE_IMAGES=$(docker images --format "table {{.Repository}}:{{.Tag}}" | grep "$REPO_NAME/sequencer-base" | grep "$BASE_VERSION")
MAIN_IMAGES=$(docker images --format "table {{.Repository}}:{{.Tag}}" | grep "$REPO_NAME/" | grep "$MAIN_VERSION")

push_images "$BASE_IMAGES"
push_images "$MAIN_IMAGES"
