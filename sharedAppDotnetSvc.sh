#!/bin/sh

start_dotnet_services() {
    export ASPNETCORE_URLS="http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT"
    #export DOTNET_URLS="http://0.0.0.0:$ADMIN_PROCESS_PORT"

    # AdminService

    cd $ADMIN_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
    rm -f app-awp-*.log
    dotnet $ADMIN_ASSEMBLY_FILE &
    echo "Started $ADMIN_SERVICE_NAME instance"
    sleep $ADMIN_WAIT_TIME_SEC

    echo -n "curl -s -L -X GET http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/healthz -> " && curl -s -L -X GET "http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/healthz" && echo ""
    echo -n "curl -X DELETE http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/messages?name=$GROUP_NAME -> " && curl -X DELETE "http://$ADMIN_PROCESS_HOST:$ADMIN_PROCESS_PORT/messages?name=$GROUP_NAME" && echo ""
    

    # SequencerService

    cd $SEQUENCER_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
    rm -f app-ss-*.log
    for i in $(seq 1 "$NB_SEQUENCERS"); do
        dotnet $SEQUENCER_ASSEMBLY_FILE &
        echo "Started $SEQUENCER_SERVICE_NAME instance number $i"
    done
    sleep $SEQUENCER_WAIT_TIME_SEC


    # ProcessorService

    cd $PROCESSOR_SERVICE_PROJECT_FOLDER/bin/$CONFIG/net8.0
    rm -f app-ps-*.log
    for i in $(seq 1 "$NB_PROCESSORS"); do
        dotnet $PROCESSOR_ASSEMBLY_FILE &
        echo "Started $PROCESSOR_SERVICE_NAME instance number $i"
    done
    sleep $PROCESSOR_WAIT_TIME_SEC
}