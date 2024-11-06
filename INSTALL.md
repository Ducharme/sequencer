
# Locally

Recommended Distro is Ubuntu 24.04

## Update package lists

```
sudo apt-get update
sudo apt-get upgrade
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

## Tools

```
sudo apt-get install -y curl, zip, unzip, jq, yq, gdb, dsniff
```

## AWS CLI version 2

Follow [Installing or updating the latest version of the AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
```
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install --update
```

Then configure [Configuration basics for CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-quickstart.html) using values from "new_user_credentials.csv" previously generated in AWS console.
```
aws configure
```

## GitHub CLI

[Installing gh on Ubuntu Linux](https://github.com/cli/cli/blob/trunk/docs/install_linux.md#debian-ubuntu-linux-raspberry-pi-os-apt)
```
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update
sudo apt install gh
```
Autenticate
```
gh auth login
```
Follow instructions for web browser

## Docker

[Docker engine install ubuntu](https://docs.docker.com/engine/install/ubuntu/#install-using-the-convenience-script)
Install using the convenience script
```
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh ./get-docker.sh
```
[Docker engine postinstall ubuntu](https://docs.docker.com/engine/install/linux-postinstall/) to run docker without admin rights
```
sudo groupadd docker
sudo usermod -aG docker $USER
newgrp docker
docker run hello-world
```

Login your account
```
docker login
```

# How To Deploy

## Download code

Clone the repository which contains all the scripts
```
git clone https://github.com/Ducharme/sequencer
```

## Configuration files

Copy/Paste the 3 files .env.example.* then rename target filenames without the .example
```
.env.development
.env.local
.env.production
```
Then edit them to set the values

## Build

### Dotnet

Run (you can use Release or Debug as argument)
```
sh buildDotnetServices.sh Release
```
Refer to [RUN.md](RUN.md)

Execute unit tests
```
dotnet test
```

### Docker

Run
```
sh buildAllDockerImages.sh
sh uploadDockerImages.sh
```
