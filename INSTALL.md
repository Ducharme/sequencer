
# Locally

## Update package lists

```
sudo apt-get update
```

## Database client

```
sudo apt-get install postgresql-client-common
sudo apt-get install -y postgresql-client
```

## Redis client

```
sudo apt-get install -y redis-tools
```

## Dotnet Core


Required for development
```
sudo apt-get install -y dotnet-sdk-8.0
```
Required for Services
```
sudo apt-get install -y dotnet-runtime-8.0
```
Required for WebServices
```
sudo apt-get install -y aspnetcore-runtime-8.0
```
Finally run
```
sudo dotnet workload update
```

## AWS CLI v2

https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html

## zip, unzip, jq, gdb, dsniff

```
sudo apt-get install -y zip, unzip, jq, gdb, dsniff
```
