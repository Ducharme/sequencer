#!/bin/sh

#sudo -s

### https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#register-the-microsoft-package-repository

# Download Microsoft signing key and repository
echo "Downloading packages-microsoft-prod.deb"
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
echo "Installing packages-microsoft-prod.deb"
sudo dpkg -i packages-microsoft-prod.deb
echo "Deleting file packages-microsoft-prod.deb"
rm packages-microsoft-prod.deb

# To avoid getting the following error the unattended-upgrades.service is stopped then disabled plus removed
# E: Could not get lock /var/lib/dpkg/lock-frontend. It is held by process 2535 (unattended-upgr)
# E: Unable to acquire the dpkg frontend lock (/var/lib/dpkg/lock-frontend), is another process using it?
sudo systemctl stop unattended-upgrades.service
sudo systemctl disable unattended-upgrades.service
sudo apt remove -y unattended-upgrades

### https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-2204
echo "Updating package cache"
sudo apt-get update
echo "Installing aspnetcore-runtime-8.0"
sudo apt-get install -y aspnetcore-runtime-8.0
sudo dotnet workload update

echo "Getting dotnet version"
dotnet --version


# https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html
#aws --version

# apt-get remove needrestart
#sudo DEBIAN_FRONTEND=noninteractive apt install redis-tools
