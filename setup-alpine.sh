#!/bin/bash

# Prompt for user input
read -p "Enter the static IP address: " ip
read -p "Enter the gateway: " gateway
read -p "Enter the netmask: " netmask
read -p "Enter DNS servers (space-separated): " dns
read -p "Enter the hostname: " hostname

# Create the answer file
cat <<EOF > alpine-setup.conf
# Example answer file for setup-alpine script

# Use US layout with US variant
KEYMAPOPTS="us us"

# Set hostname
HOSTNAMEOPTS="-n $hostname"

# Contents of /etc/network/interfaces
INTERFACESOPTS="auto lo
iface lo inet loopback

auto eth0
iface eth0 inet static
    address $ip
    netmask $netmask
    gateway $gateway
    hostname $hostname
"

# DNS settings
DNSOPTS="-d example.com $dns"

# Set timezone to UTC
TIMEZONEOPTS="-z UTC"

# Uncomment if you need a proxy
PROXYOPTS=""

# Add a random mirror
APKREPOSOPTS="-r"

# Install Openssh
SSHDOPTS="-c openssh"

# Use openntpd
NTPOPTS="-c openntpd"

# Use /dev/sda as the default disk
DISKOPTS="-m sys /dev/sda"

EOF

# Run setup-alpine with the generated answer file
setup-alpine -f alpine-setup.conf
