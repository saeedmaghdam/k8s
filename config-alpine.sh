# configure alpine

# set the timezone to amsterdam
ln -sf /usr/share/zoneinfo/Europe/Amsterdam /etc/localtime
echo "Europe/Amsterdam" > /etc/timezone

# update and upgrade
apk update
apk upgrade

# install openssh and configure it, should be able to login as root with password
sed -i 's/#PermitRootLogin prohibit-password/PermitRootLogin yes/' /etc/ssh/sshd_config
sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/' /etc/ssh/sshd_config
sed -i 's/#PermitEmptyPasswords no/PermitEmptyPasswords yes/' /etc/ssh/sshd_config
rc-service sshd restart
rc-update add sshd

# install bash
apk add bash

# install sudo
apk add sudo
echo "root ALL=(ALL) ALL" > /etc/sudoers

# install ping, netcat, curl, wget, vim, git, bash
apk add iputils netcat-openbsd curl wget vim git bash

# install k3s
curl -sfL https://get.k3s.io | sh -

# cat config file, replace server with the IP in the config file
# Get the IP address of eth0
ETH0_IP=$(ip addr show eth0 | grep 'inet ' | awk '{print $2}' | cut -d '/' -f 1)

# File path to the k3s config
K3S_CONFIG="/etc/rancher/k3s/k3s.yaml"

# Check if the file exists
if [ ! -f "$K3S_CONFIG" ]; then
  echo "Config file not found: $K3S_CONFIG"
  exit 1
fi

# Backup the original config file
cp "$K3S_CONFIG" "${K3S_CONFIG}.backup"

# Replace the server address with eth0 IP
sed -i "s|server: https://127.0.0.1:6443|server: https://$ETH0_IP:6443|g" "$K3S_CONFIG"

# Print the updated config to the console
cat "$K3S_CONFIG"

# Inform the user
echo "Updated the server address in $K3S_CONFIG and printed the result."