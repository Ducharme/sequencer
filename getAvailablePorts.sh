#!/bin/sh

getAvailablePorts() {
    start_port=$1
    count=$2
    available_ports=""
    current_port=$start_port
    found=0

    while [ $found -lt $count ]; do
        if ! nc -z localhost "$current_port" >/dev/null 2>&1; then
            available_ports="$available_ports,$current_port"
            found=$((found + 1))
        fi
        current_port=$((current_port + 1))
    done

    aps=$(echo "$available_ports" | sed 's/^,*//' | sed 's/,*$//')
    echo "$aps"
}

launchDotnetApp() {
    app_dll=$1
    port=$2
    echo "Attempting to start $app_dll on port $port" >&2
    nohup dotnet $app_dll --urls="http://localhost:$port" > /dev/null 2>&1 &
    return 0
}

launchDotnetAppOnAvailablePorts() {
    start_port=$1
    port_count=$2
    app_dll=$3

    if [ -z "$start_port" ] || [ -z "$port_count" ] || [ -z "$app_dll" ]; then
        echo "Usage: launchDotnetAppOnAvailablePorts <start_port> <port_count> <app_dll>" >&2
        return 1
    fi

    available_ports=$(getAvailablePorts "$start_port" "$port_count")
    echo "available_ports: $available_ports" >&2
    ports=$(echo "$available_ports" | tr ',' '\n')
    for port in $ports; do
        launchDotnetApp "$app_dll" "$port"
    done
    echo "$available_ports"
}

# Usage example when in the assembly folder
# launchDotnetAppOnAvailablePorts 5000 5 "app.dll"
