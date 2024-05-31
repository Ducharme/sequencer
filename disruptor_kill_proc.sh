#!/bin/sh


export RUN_ENV_FILE=.env.local

. ./extractTestAllValues.sh

FOLDER=$(pwd)


kill_services() {
    service_name=$1
    PSS_ID=$(ps -aux | grep "dotnet $service_name" | grep -wv "grep" | awk '{print $2}' | tr -s '\r\n' ' ')
    if [ ! -z "$PSS_ID" ]; then echo "Killing process $service_name with pid $PSS_ID" && kill -15 $PSS_ID; fi
}

# Call the function with an array of service names
sequencer_services="SequencerWebService.dll SequencerService.dll"
for service in $sequencer_services; do
    kill_services "$service"
done

# Call the function with an array of service names
processor_services="ProcessorWebService.dll ProcessorService.dll"
for service in $processor_services; do
    kill_services "$service"
done


. ./recoverDotnetServicesLocally.sh

cd $FOLDER

echo "DONE DISRUPTING!"
