#!/bin/sh

waitForCompletion() {
    expectedCount=$1
    containerHost=$2
    containerPort=$3
    groupName=$4

    local success=1 # Initialize to 1 (failure), will set to 0 if we reach the count
    local iteration=0
    local lastCount=0
    local unchangedCount=0

    while [ $lastCount -lt $expectedCount ]; do
        iteration=$((iteration + 1))
        echo -n "curl -s -X GET http://$containerHost:$containerPort/list/sequenced/count?name=$groupName (#$iteration) -> "
        output=$(curl -s -X GET http://$containerHost:$containerPort/list/sequenced/count?name=$groupName)
        status=$?
        if [ $status -eq 0 ]; then
            echo "$output of $expectedCount"
            if [ "$output" = "$expectedCount" ]; then
                success=0 # Set to 0 (success) if we reach the expected count
                break # Exit the loop early on success
            fi

            # Check if the count has increased
            if [ "$output" -eq "$lastCount" ]; then
                unchangedCount=$((unchangedCount + 1))
                if [ $unchangedCount -ge 3 ]; then
                    echo "Count hasn't increased in 3 iterations, exiting..."
                    break
                fi
            else
                unchangedCount=0
                lastCount=$output
            fi
        else
            echo "Failed (exit code $status)"
        fi
        sleep 1
    done
    return $success
}
