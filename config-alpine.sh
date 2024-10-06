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
# store eth0 ip in a variable
ETH0_IP=$(ip -4 addr show eth0 | grep -oP '(?<=inet\s)\d+(\.\d+){3}')
seq -i 's/127.0.0.1/10.0.1.112/g' /etc/rancher/k3s/k3s.yaml > /etc/rancher/k3s/k3s.yaml
cat /etc/rancher/k3s/k3s.yaml