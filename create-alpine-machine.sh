#!/bin/bash

# Set to exit if any command fails
set -e

echo "Enter the VM_ID: "
read VM_ID

echo "Enter the cluster ID: "
read CLUSTER_ID

echo "Enter Alpine image URL (default https://dl-cdn.alpinelinux.org/alpine/v3.20/releases/x86_64/alpine-virt-3.20.3-x86_64.iso): "
read IMAGE
if [ -z "$IMAGE" ]; then
  IMAGE=https://dl-cdn.alpinelinux.org/alpine/v3.20/releases/x86_64/alpine-virt-3.20.3-x86_64.iso
fi
IMAGE_FILENAME=$(basename $IMAGE)

#----------------------------------------------------------------------

# Settings that have to change each time a VM is created.
#===================
NAME=k8s-cluster-$CLUSTER_ID
IP=10.0.1.$VM_ID/8
#IP=dhcp
#===================

MEMORY=1536
CORE=1
DEST=local-lvm
DISK_SIZE=10
DISK_NAME=vm-$VM_ID-disk-0
DISK_ID=$DEST:$DISK_NAME
DISK_DEVICE=scsi0

USER=saeed

GW=10.0.0.1
NAMESERVER=8.8.8.8

#----------------------------------------------------------------------

# Function to check if the VM already exists
check_vm_exists() {
  qm status $VM_ID &> /dev/null
}

# If the VM exists, exit with a message
if check_vm_exists; then
  echo "VM $VM_ID already exists. Please choose another VM ID or delete the existing VM."
  exit 1
fi

# Check if ISO is already downloaded, if not, download it
if [ ! -f /var/lib/vz/template/iso/$IMAGE_FILENAME ]; then
  echo "Downloading ISO image..."
  wget $IMAGE -P /var/lib/vz/template/iso/
else
  echo "ISO image already exists."
fi

# Create the VM
echo "Creating VM $VM_ID..."
qm create $VM_ID --memory $MEMORY --core $CORE --name $NAME --net0 e1000,bridge=vmbr0

# Add a disk with SSD emulation and virtio-scsi-pci
echo "Adding disk to VM..."
qm set $VM_ID --scsi0 $DEST:$DISK_SIZE,ssd=1 --scsihw virtio-scsi-pci

# Attach the Alpine ISO as CD-ROM
echo "Attaching ISO as CD-ROM..."
qm set $VM_ID --ide2 local:iso/$IMAGE_FILENAME,media=cdrom

# Add cloud-init disk
echo "Adding cloud-init disk..."
qm set $VM_ID --ide3 $DEST:cloudinit

# Set boot order
qm set $VM_ID --boot order='scsi0;ide2'

# Configure serial console and graphics
echo "Configuring serial console and graphics..."
qm set $VM_ID --serial0 socket --vga virtio

# Set machine type to q35
echo "Setting machine type to q35..."
qm set $VM_ID --machine q35

# Configure static IP and gateway
echo "Configuring network settings (IP and gateway)..."
qm set $VM_ID --ipconfig0 ip=$IP,gw=$GW

# Enable QEMU agent
echo "Enabling QEMU guest agent..."
qm set $VM_ID --agent enabled=1

# Set nameserver
echo "Configuring nameserver..."
qm set $VM_ID --nameserver $NAMESERVER

# Disable autostart on boot
echo "Disabling autostart on boot..."
qm set $VM_ID --onboot 0

# Set cloud-init user and SSH keys
echo "Configuring cloud-init user and SSH keys..."
qm set $VM_ID --ciuser $USER --sshkeys=/root/.ssh/id_rsa.pub

# Start the VM
echo "Starting VM $VM_ID..."
qm start $VM_ID

echo "VM $VM_ID is created and started successfully."
