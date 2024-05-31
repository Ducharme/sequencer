#!/bin/sh

sh stopRedisLocally.sh
sh stopPostgresqlLocally.sh
sh killDotnetServicesLocally.sh
sh killDockerServicesLocally.sh

echo "DONE HALTING!"
