#!/bin/sh

# docker login

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

BASE_IMAGES_ALL=$(docker images --format "table {{.Repository}}:{{.Tag}}" | grep "$REPO_NAME/sequencer-base")
BASE_IMAGES_VERSIONS=$(echo "$BASE_IMAGES_ALL" | grep -v "<none>" | cut -d':' -f2 | cut -d'-' -f1)
BASE_IMAGES_LATEST_VERSION=$(echo "$BASE_IMAGES_VERSIONS" | tr ' ' '\n' | sort -V | tail -n1)
BASE_IMAGES=$(echo "$BASE_IMAGES_ALL" | grep "$BASE_IMAGES_LATEST_VERSION")
echo "BASE_IMAGES_ALL"
echo "$BASE_IMAGES_ALL"
echo ""

MAIN_IMAGES_ALL=$(docker images --format "table {{.Repository}}:{{.Tag}}" | grep "$REPO_NAME/" | grep -v "sequencer-base" | grep -v "<none>")
MAIN_IMAGES_VERSIONS=$(echo "$MAIN_IMAGES_ALL" | grep -v "<none>" | cut -d':' -f2 | cut -d'-' -f1)
MAIN_IMAGES_LATEST_VERSION=$(echo "$MAIN_IMAGES_VERSIONS" | tr ' ' '\n' | sort -V | tail -n1)
MAIN_IMAGES=$(echo "$MAIN_IMAGES_ALL" | grep "$MAIN_IMAGES_LATEST_VERSION")
echo "MAIN_IMAGES"
echo "$MAIN_IMAGES"
echo ""

push_images "$BASE_IMAGES"
push_images "$MAIN_IMAGES"
