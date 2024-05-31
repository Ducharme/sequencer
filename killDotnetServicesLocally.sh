#!/bin/sh

kill_services() {
    service_name=$1
    PSS_ID=$(ps -aux | grep "dotnet $service_name" | grep -wv "grep" | awk '{print $2}' | tr -s '\r\n' ' ')
    if [ ! -z "$PSS_ID" ]; then echo "Killing process $service_name with pid $PSS_ID" && kill -15 $PSS_ID; fi
}

# Call the function with an array of service names
services="AdminWebPortal.dll AdminService.dll ProcessorWebService.dll ProcessorService.dll SequencerWebService.dll SequencerService.dll"
for service in $services; do
    kill_services "$service"
done
