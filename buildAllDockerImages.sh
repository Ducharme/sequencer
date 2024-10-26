#!/bin/sh

BASE_VERSION=0.0.2
DOTNET_SDK_VERSION=8.0.402-bookworm-slim
DOTNET_RUNTIME_VERSION=8.0.8-bookworm-slim
DOTNET_ASPNET_VERSION=8.0.8-bookworm-slim
DD_TRACER_VERSION=3.3.1
REPO_NAME=claudeducharme

FOLDER=$(pwd)

build_arg_1=$1 # --progress=plain
build_arg_2=$2 # --no-cache 

#DD_TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-)

echo "Building arguments: $build_arg_1 $build_arg_2"

get_latest_version() {
    local image_name=$1
    #curl -L -s "https://hub.docker.com/v2/repositories/claudeducharme/sequencerwebservice/tags?page_size=5" | jq -r '.results[0].name' | cut -d'-' -f1
    local link="https://hub.docker.com/v2/repositories/$REPO_NAME/$image_name/tags?page_size=5"
    local version=$(curl -L -s "$link" | jq -r '.results[0].name' | cut -d'-' -f1)
    if [ $? -ne 0 ]; then
        echo "Could not fetch latest image version from $link"
        exit 3
    fi
    echo "$version"
}

# Function to split string into arguments
split_args() {
    old_ifs=$IFS
    IFS=' '
    set -- $1
    IFS=$old_ifs
    for arg in "$@"; do
        printf '%s\n' "$arg"
    done
}

check_and_pull_or_build_image() {
    image_name=$1
    image_tag=$2
    docker_file=$3
    build_arg_0=$4

    DOCKER_IMAGES=$(docker images "$image_name:$image_tag")
    if echo "$DOCKER_IMAGES" | grep -q "$image_name"; then BASE_EXISTS_LOCALLY=TRUE; else BASE_EXISTS_LOCALLY=FALSE; fi
    if [ "$BASE_EXISTS_LOCALLY" = "FALSE" ]; then
        echo "Image $image_name:$image_tag not found locally"
        echo "Checking if image exists https://hub.docker.com/v2/repositories/$REPO_NAME/$image_name/tags/$image_tag"
        BASE_INFO=$(curl -s "https://hub.docker.com/v2/repositories/$REPO_NAME/$image_name/tags/$image_tag")
        if echo "$BASE_INFO" | grep -q "digest"; then
            BASE_EXISTS_HUB=TRUE
        else
            BASE_EXISTS_HUB=FALSE
        fi
        
        if [ "$BASE_EXISTS_HUB" = "TRUE" ]; then
            echo "docker pull $REPO_NAME/$image_name:$image_tag"
            if docker pull "$REPO_NAME/$image_name:$image_tag"; then
                echo "Image $image_name:$image_tag pulled successfully"
            else
                echo "Failed to pull image $image_name:$image_tag"
                exit 1
            fi
        else
            # Build the --build-arg options
            local build_args=""
            for arg in $(split_args "$build_arg_0")
            do
                build_args="$build_args --build-arg $arg"
            done
            echo "build_args=$build_args"

            echo "docker build -f $docker_file -t $image_name:$image_tag $build_args $build_arg_1 $build_arg_2 ."
            docker build -f $docker_file -t "$image_name:$image_tag" $build_args $build_arg_1 $build_arg_2 .
            docker tag "$image_name:$image_tag" "$REPO_NAME/$image_name:$image_tag"
            if [ $? -eq 0 ]; then
                echo "Image $image_name:$image_tag built successfully"
            else
                echo "Failed to build image $image_name:$image_tag"
                exit 2
            fi
        fi
    else
        echo "Image $image_name:$image_tag found locally"
    fi
}

AWP_VER=$(get_latest_version "adminwebportal")
PWS_VER=$(get_latest_version "processorwebservice")
SWS_VER=$(get_latest_version "sequencerwebservice")
LAST_MAIN_VERSION=$(echo "$AWP_VER $PWS_VER $SWS_VER" | tr ' ' '\n' | sort -V | tail -n1)
MAIN_VERSION=$(echo "$LAST_MAIN_VERSION" | awk -F. '{$3++; print $1"."$2"."$3}')
echo "MAIN_VERSION=$MAIN_VERSION BASE_VERSION=$BASE_VERSION DOTNET_SDK_VERSION=$DOTNET_SDK_VERSION DOTNET_RUNTIME_VERSION=$DOTNET_RUNTIME_VERSION DOTNET_ASPNET_VERSION=$DOTNET_ASPNET_VERSION DD_TRACER_VERSION=$DD_TRACER_VERSION REPO_NAME=$REPO_NAME"

BASE_IMAGE_NAME=sequencer-base
BASE_DEP_TAG="datadog$DD_TRACER_VERSION"
BASE_IMAGE_TAG="$BASE_VERSION-$BASE_DEP_TAG"
echo "Handling $BASE_IMAGE_NAME..."
check_and_pull_or_build_image "$BASE_IMAGE_NAME" "$BASE_IMAGE_TAG" "./Dockerfiles/Dockerfile-Base" "DD_TRACER_VERSION=$DD_TRACER_VERSION"

# sequencer-compile is always rebuilt to account for any change in code files or scritps
COMPILE_IMAGE_NAME=sequencer-compile
SDK_IMAGE="mcr.microsoft.com/dotnet/sdk:$DOTNET_SDK_VERSION"
SDK_DEP_TAG="$DOTNET_SDK_VERSION"
COMPILE_IMAGE_TAG=$MAIN_VERSION-$SDK_DEP_TAG
SDK_COMPILE_ARGS="--build-arg BASE_IMAGE=$SDK_IMAGE $build_arg_1 $build_arg_2"
echo "Handling $COMPILE_IMAGE_NAME..."
docker build -f ./Dockerfiles/Dockerfile-Compile -t $COMPILE_IMAGE_NAME:$COMPILE_IMAGE_TAG $SDK_COMPILE_ARGS .
if [ ! $? -eq 0 ]; then echo "Build of $COMPILE_IMAGE_NAME failed, exiting" && exit 1; fi

# DOTNET Services

RUNTIME_IMAGE="mcr.microsoft.com/dotnet/runtime:$DOTNET_RUNTIME_VERSION"
RUNTIME_DEP_TAG="runtime$DOTNET_RUNTIME_VERSION-datadog$DD_TRACER_VERSION"
RUNTIME_IMAGE_NAME="sequencer-base-runtime"
RUNTIME_IMAGE_TAG="$BASE_VERSION-$RUNTIME_DEP_TAG"
echo "check_and_pull_or_build_image $RUNTIME_IMAGE_NAME $RUNTIME_IMAGE_TAG ./Dockerfiles/Dockerfile-Base-Runtime BASE_IMAGE=$BASE_IMAGE_NAME:$BASE_IMAGE_TAG RUNTIME_IMAGE=$RUNTIME_IMAGE"
check_and_pull_or_build_image "$RUNTIME_IMAGE_NAME" "$RUNTIME_IMAGE_TAG" "./Dockerfiles/Dockerfile-Base-Runtime" "BASE_IMAGE=$BASE_IMAGE_NAME:$BASE_IMAGE_TAG RUNTIME_IMAGE=$RUNTIME_IMAGE"

BUILD_RUNTIME_ARGS="--build-arg BASE_IMAGE_RUNTIME=$RUNTIME_IMAGE_NAME:$RUNTIME_IMAGE_TAG --build-arg COMPILE_IMAGE=$COMPILE_IMAGE_NAME:$COMPILE_IMAGE_TAG --build-arg BASE_IMAGE_RUNTIME=$REPO_NAME/$RUNTIME_IMAGE_NAME:$RUNTIME_IMAGE_TAG $build_arg_1 $build_arg_2"
echo "docker build -f ./Dockerfiles/Dockerfile-ProcessorService -t processorservice:latest -t $REPO_NAME/processorservice:$MAIN_VERSION-$RUNTIME_DEP_TAG $BUILD_RUNTIME_ARGS ."
docker build -f ./Dockerfiles/Dockerfile-ProcessorService -t "processorservice:latest" -t "$REPO_NAME/processorservice:$MAIN_VERSION-$RUNTIME_DEP_TAG" $BUILD_RUNTIME_ARGS .
if [ ! $? -eq 0 ]; then echo "Build of Dockerfile-ProcessorService failed, exiting" && exit 1; fi

echo "docker build -f ./Dockerfiles/Dockerfile-SequencerService -t sequencerservice:latest -t $REPO_NAME/sequencerservice:$MAIN_VERSION-$RUNTIME_DEP_TAG $BUILD_RUNTIME_ARGS ."
docker build -f ./Dockerfiles/Dockerfile-SequencerService -t "sequencerservice:latest" -t "$REPO_NAME/sequencerservice:$MAIN_VERSION-$RUNTIME_DEP_TAG" $BUILD_RUNTIME_ARGS .
if [ ! $? -eq 0 ]; then echo "Build of Dockerfile-SequencerService failed, exiting" && exit 1; fi

# ASPNET Web Services

ASPNET_IMAGE="mcr.microsoft.com/dotnet/aspnet:$DOTNET_ASPNET_VERSION"
ASPNET_DEP_TAG="aspnet$DOTNET_ASPNET_VERSION-datadog$DD_TRACER_VERSION"
ASPNET_IMAGE_NAME=sequencer-base-aspnet
ASPNET_IMAGE_TAG="$BASE_VERSION-$ASPNET_DEP_TAG"
echo "check_and_pull_or_build_image $ASPNET_IMAGE_NAME $ASPNET_IMAGE_TAG ./Dockerfiles/Dockerfile-Base-Aspnet BASE_IMAGE=$REPO_NAME/$BASE_IMAGE_NAME:$BASE_IMAGE_TAG ASPNET_IMAGE=$ASPNET_IMAGE"
check_and_pull_or_build_image "$ASPNET_IMAGE_NAME" "$ASPNET_IMAGE_TAG" "./Dockerfiles/Dockerfile-Base-Aspnet" "BASE_IMAGE=$REPO_NAME/$BASE_IMAGE_NAME:$BASE_IMAGE_TAG ASPNET_IMAGE=$ASPNET_IMAGE"

BUILD_APSNET_ARGS="--build-arg BASE_IMAGE_ASPNET=$ASPNET_IMAGE_NAME:$ASPNET_IMAGE_TAG --build-arg COMPILE_IMAGE=$COMPILE_IMAGE_NAME:$COMPILE_IMAGE_TAG --build-arg BASE_IMAGE_ASPNET=$REPO_NAME/$ASPNET_IMAGE_NAME:$ASPNET_IMAGE_TAG $build_arg_1 $build_arg_2"
echo "docker build -f ./Dockerfiles/Dockerfile-AdminWebPortal -t adminwebportal:latest -t $REPO_NAME/adminwebportal:$MAIN_VERSION-$ASPNET_DEP_TAG $BUILD_APSNET_ARGS ."
docker build -f ./Dockerfiles/Dockerfile-AdminWebPortal -t "adminwebportal:latest" -t "$REPO_NAME/adminwebportal:$MAIN_VERSION-$ASPNET_DEP_TAG" $BUILD_APSNET_ARGS .
if [ ! $? -eq 0 ]; then echo "Build of Dockerfile-AdminWebPortal failed, exiting" && exit 1; fi

echo "docker build -f ./Dockerfiles/Dockerfile-ProcessorWebService -t processorwebservice:latest -t $REPO_NAME/processorwebservice:$MAIN_VERSION-$ASPNET_DEP_TAG $BUILD_APSNET_ARGS ."
docker build -f ./Dockerfiles/Dockerfile-ProcessorWebService -t "processorwebservice:latest" -t "$REPO_NAME/processorwebservice:$MAIN_VERSION-$ASPNET_DEP_TAG" $BUILD_APSNET_ARGS .
if [ ! $? -eq 0 ]; then echo "Build of Dockerfile-ProcessorWebService failed, exiting" && exit 1; fi

echo "docker build -f ./Dockerfiles/Dockerfile-SequencerWebService -t sequencerwebservice:latest -t $REPO_NAME/sequencerwebservice:$MAIN_VERSION-$ASPNET_DEP_TAG $BUILD_APSNET_ARGS ."
docker build -f ./Dockerfiles/Dockerfile-SequencerWebService -t "sequencerwebservice:latest" -t "$REPO_NAME/sequencerwebservice:$MAIN_VERSION-$ASPNET_DEP_TAG" $BUILD_APSNET_ARGS .
if [ ! $? -eq 0 ]; then echo "Build of Dockerfile-SequencerWebService failed, exiting" && exit 1; fi

echo "DONE BUILDING!"
