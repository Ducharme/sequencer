#!/bin/sh

start_dotnet_web_services() {

    # AdminWebPortal

    cd $ADMIN_WEB_PORTAL_PROJECT_FOLDER/bin/$CONFIG/net8.0
    rm -f app-awp-*.log
    AWP_PORTS=$(launchDotnetAppOnAvailablePorts 5710 1 $ADMIN_WEB_ASSEMBLY_FILE)
    sleep $ADMIN_WAIT_TIME_SEC
    export ADMIN_PROCESS_PORT=$(echo "$AWP_PORTS" | tr ',' '\n' | head -n 1)
    echo -n "curl -s -L -X GET http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/healthz -> " && curl -s -L -X GET "http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/healthz"
    echo -n "curl -X DELETE http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/messages?name=$GROUP_NAME -> " && curl -X DELETE "http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/messages?name=$GROUP_NAME"
    echo ""

    # SequencerWebService

    cd $SEQUENCER_WEB_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
    rm -f app-ss-*.log
    SWS=$(launchDotnetAppOnAvailablePorts 5720 $NB_SEQUENCERS $SEQUENCER_WEB_ASSEMBLY_FILE)
    sleep $SEQUENCER_WAIT_TIME_SEC

    # ProcessorWebService

    cd $PROCESSOR_WEB_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
    rm -f app-ps-*.log
    PWS=$(launchDotnetAppOnAvailablePorts 5750 $NB_PROCESSORS $PROCESSOR_WEB_ASSEMBLY_FILE)
    sleep $PROCESSOR_WAIT_TIME_SEC

}