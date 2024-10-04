#!/bin/sh

check_image_exists() {
    img_name=$1

    if ! docker images | grep -q "$img_name"; then
        echo "Docker image \"$pws_name\" does not exist. Please build the image and try again."
        exit 1
    fi
}

check_images_exist() {
    awp_name=$1
    pws_name=$2
    sws_name=$3

    check_image_exists $awp_name
    check_image_exists $pws_name
    check_image_exists $sws_name
}

start_app_containers() {
    local awp_name=$1
    local pws_name=$2
    local sws_name=$3

    local DEFAULT_CONTAINER_PORT=8080
    export AWP_CONTAINER_PORT=$DEFAULT_CONTAINER_PORT
    export PWS_CONTAINER_PORT=$DEFAULT_CONTAINER_PORT
    export SWS_CONTAINER_PORT=$DEFAULT_CONTAINER_PORT

    . ./setEnvFileValues.sh
    . ./getContainerIP.sh
    setDependenciesEndpoints
    
    EP_ENVS="-e REDIS_ENDPOINT=$REDIS_ENDPOINT -e PGSQL_ENDPOINT=$PGSQL_ENDPOINT"
    DD_ENVS="-e DD_API_KEY=$DD_API_KEY -e CORECLR_ENABLE_PROFILING=1 -e CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8} -e CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so -e DD_DOTNET_TRACER_HOME=/opt/datadog -e DD_PROCESS_AGENT_ENABLED=true"
    LOGGING="--log-driver json-file"
    . ./waitForContainer.sh


    # AdminWebPortal

    echo "docker run --name $awp_name -d $LOGGING $EP_ENVS <DatadogEnvVars> $awp_name" && docker run --name $awp_name -d $LOGGING $EP_ENVS $DD_ENVS $awp_name
    sleep $DOCKER_START_DELAY
    export AWP_CONTAINER_HOST=$(getContainerIpFromImageName "$awp_name")
    echo "AWP_CONTAINER_HOST=$AWP_CONTAINER_HOST and AWP_CONTAINER_PORT=$AWP_CONTAINER_PORT"
    waitForContainer $ADMIN_WAIT_TIME_SEC $AWP_CONTAINER_HOST $AWP_CONTAINER_PORT

    echo -n "curl -X DELETE http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/messages?name=$GROUP_NAME -> " && curl -X DELETE "http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/messages?name=$GROUP_NAME" && echo "\n"
    


    # SequencerService

    for i in $(seq 1 "$NB_SEQUENCERS"); do
        echo -n "docker run $LOGGING -d $EP_ENVS <DatadogEnvVars> $sws_name (#$i) -> " && docker run $LOGGING -d $EP_ENVS $DD_ENVS $sws_name
    done
    sleep $DOCKER_START_DELAY
    export SWS_CONTAINER_HOSTS=$(getContainerIpsFromImageName "$sws_name")
    SWS_CONTAINER_HOSTS_INLINE=$(convert_to_comma_separated "$SWS_CONTAINER_HOSTS")
    echo "SWS_CONTAINER_HOSTS=$SWS_CONTAINER_HOSTS_INLINE and SWS_CONTAINER_PORT=$SWS_CONTAINER_PORT"
    waitForContainers $ADMIN_WAIT_TIME_SEC $SWS_CONTAINER_HOSTS\$SWS_CONTAINER_PORT


    # ProcessorService

    for i in $(seq 1 "$NB_PROCESSORS"); do
        echo -n "docker run $LOGGING -d <DatadogEnvVars> $pws_name (#$i) -> " && docker run $LOGGING -d $EP_ENVS $DD_ENVS $pws_name
    done
    sleep $DOCKER_START_DELAY
    export PWS_CONTAINER_HOSTS=$(getContainerIpsFromImageName "$pws_name")
    PWS_CONTAINER_HOSTS_INLINE=$(convert_to_comma_separated "$PWS_CONTAINER_HOSTS")
    echo "PWS_CONTAINER_HOSTS=$PWS_CONTAINER_HOSTS_INLINE and PWS_CONTAINER_PORT=$PWS_CONTAINER_PORT"
    waitForContainers $ADMIN_WAIT_TIME_SEC $PWS_CONTAINER_HOSTS $PWS_CONTAINER_PORT


    # Check health

    FIRST_PWS_HOST=$(get_first_item "$PWS_CONTAINER_HOSTS_INLINE")
    FIRST_SWS_HOST=$(get_first_item "$SWS_CONTAINER_HOSTS_INLINE")
    echo ""
    echo -n "curl -s -L -X GET http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/healthz ($awp_name) -> " && curl -s -L -X GET "http://$AWP_CONTAINER_HOST:$AWP_CONTAINER_PORT/healthz" && echo ""
    echo -n "curl -s -L -X GET http://$FIRST_PWS_HOST:$PWS_CONTAINER_PORT/healthz ($pws_name) -> " && curl -s -L -X GET "http://$FIRST_PWS_HOST:$PWS_CONTAINER_PORT/healthz" && echo ""
    echo -n "curl -s -L -X GET http://$FIRST_SWS_HOST:$SWS_CONTAINER_PORT/healthz ($pws_name) -> " && curl -s -L -X GET "http://$FIRST_SWS_HOST:$SWS_CONTAINER_PORT/healthz" && echo ""
}
