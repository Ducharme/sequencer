#!/bin/sh

CLOSE_METHOD=TCPKILL # SS or TCPKILL or GDB or CUTTER

export RUN_ENV_FILE=.env.local

. ./extractTestAllValues.sh

if [ "$CLOSE_METHOD" = "SS" ]; then
    WHICH_SS=$(which ss)
    if [ -z "$WHICH_SS" ]; then
        echo "ss command cannot be found, install with 'sudo apt install iproute2', exiting"
        exit 2
    else
        echo "ss from $WHICH_SS will be used"
    fi
elif [ "$CLOSE_METHOD" = "TCPKILL" ]; then
    WHICH_TK=$(which tcpkill)
    if [ -z "$WHICH_TK" ]; then
        echo "tcpkill command cannot be found, install with 'sudo apt-get install dsniff', exiting"
        exit 2
    else
        echo "tcpkill from $WHICH_TK will be used"
    fi
elif [ "$CLOSE_METHOD" = "GDB" ]; then
    WHICH_GDB=$(which gdb)
    if [ -z "$WHICH_GDB" ]; then
        echo "gdb command cannot be found, install with 'sudo apt install gdb', exiting"
        exit 2
    else
        echo "gdb from $WHICH_GDB will be used"
    fi
elif [ "$CLOSE_METHOD" = "CUTTER" ]; then
    WHICH_CT=$(which cutter)
    if [ -z "$WHICH_CT" ]; then
        echo "cutter command cannot be found, install with 'sudo apt install cutter', exiting"
        exit 2
    else
        echo "cutter from $WHICH_CT will be used"
    fi
else
    echo "Invalid CLOSE_METHOD=$CLOSE_METHOD"
    exit 1
fi


FOLDER=$(pwd)
mkdir -p .tmp

kill_connections() {
    service_name=$1
    port=6379

    PSS_ID=$(ps -aux | grep "dotnet $service_name" | grep -wv "grep" | awk '{print $2}')
    PSS_ID_STR=$(echo "$PSS_ID" | tr -s '\r\n' ' ')
    echo "PROC_IDS=$PSS_ID_STR"
    echo "$PSS_ID" | while read pid; do
        if [ ! -z "$pid" ]; then
            # ss -> ESTAB 0 0 172.17.0.1:35326 172.17.0.3:redis users:(("dotnet",pid=3clear4810,fd=112))
            # ss -> ESTAB 0 0 172.17.0.1:35327 172.17.0.3:redis users:(("dotnet",pid=3clear4810,fd=115))
            SS_RES=$(ss -tpH dst :"$port" | grep ",pid=$pid,")
            echo "$SS_RES" | while read conn; do
                LCL_ADDR=$(echo "$conn" | awk '{print $4}')
                REM_ADDR=$(echo "$conn" | awk '{print $5}')

                SRC_IP=$(echo "$LCL_ADDR" | cut -d ':' -f1)
                SRC_PORT=$(echo "$LCL_ADDR" | cut -d ':' -f2)
                DST_IP=$(echo "$REM_ADDR" | cut -d ':' -f1)
                DST_PORT=$(echo "$REM_ADDR" | cut -d ':' -f2)

                if [ "$CLOSE_METHOD" = "SS" ]; then
                    echo "sudo ss -K src $SRC_IP sport $SRC_PORT"
                    sudo ss -K src $SRC_IP sport $SRC_PORT >> .tmp/ss.log
                elif [ "$CLOSE_METHOD" = "TCPKILL" ]; then
                    # To get a password prompt otherwise the timeout will mess the prompt
                    sudo tcpkill >> .tmp/tcpkill.log
                    ENI=$(ip addr show | grep $SRC_IP | awk '{print $7}')
                    echo "sudo timeout 1s tcpkill -i any host $SRC_IP and port $SRC_PORT"
                    sudo timeout 0.6s tcpkill -i any -9 host $SRC_IP and port $SRC_PORT >> .tmp/tcpkill.log
                elif [ "$CLOSE_METHOD" = "GDB" ]; then
                    FD=$(echo "$conn" | awk '{print $6}' | cut -d ',' -f3 | tr -d ')' | cut -d '=' -f2)
                    echo "sudo gdb -q -p \"$pid\" -ex \"call close($FD)\" -ex \"detach\" -ex \"quit\""
                    sudo gdb -q -p "$pid" -ex "call close($FD)" -ex "detach" -ex "quit" >> .tmp/gdb.log
                elif [ "$CLOSE_METHOD" = "CUTTER" ]; then
                    # Not working: cutter always returns "Invalid IP address"
                    echo "sudo cutter $LCL_ADDR $REM_ADDR"
                    sudo cutter $LCL_ADDR $REM_ADDR
                fi
            done
        fi
    done
}

# Call the function with an array of service names
sequencer_services="SequencerWebService.dll SequencerService.dll"
for service in $sequencer_services; do
    kill_connections "$service"
done

# Call the function with an array of service names
processor_services="ProcessorWebService.dll ProcessorService.dll"
for service in $processor_services; do
    kill_connections "$service"
done


. ./recoverDotnetServicesLocally.sh

cd $FOLDER

echo "DONE DISRUPTING!"
