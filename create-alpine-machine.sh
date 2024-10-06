#!/bin/bash

# source: https://docs.technotim.live/posts/cloud-init-cloud-image/
# source: https://pve.proxmox.com/pve-docs/qm.1.html

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
# DEST=TrueNAS
DISK_SIZE=+10G
DISK_NAME=vm-$VM_ID-disk-0
DISK_ID=$DEST:$DISK_NAME
DISK_DEVICE=scsi0

USER=saeed

GW=10.0.0.1
NAMESERVER=8.8.8.8
#----------------------------------------------------------------------

if [ ! -f $IMAGE_FILENAME ]; then
  wget $IMAGE
fi

qm create $VM_ID --memory $MEMORY --core $CORE --name $NAME --net0 e1000,bridge=vmbr0
qm importdisk $VM_ID $IMAGE_FILENAME $DEST --format qcow2
qm set $VM_ID --scsihw virtio-scsi-pci --$DISK_DEVICE $DISK_ID
qm resize $VM_ID $DISK_DEVICE $DISK_SIZE
qm set $VM_ID --ide2 $DEST:cloudinit
qm set $VM_ID --boot c --bootdisk $DISK_DEVICE
qm set $VM_ID --serial0 socket --vga virtio
qm set $VM_ID --machine q35

qm set $VM_ID --ipconfig0 ip=$IP,gw=$GW
#qm set $VM_ID --ipconfig0 ip=dhcp
qm set $VM_ID --agent enabled=1
# qm set $VM_ID --autostart 1  #on crash
qm set $VM_ID --nameserver $NAMESERVER
qm set $VM_ID --onboot 0  # start VM on boot of host
qm set $VM_ID --ciuser $USER
qm set $VM_ID --sshkeys=/root/.ssh/id_rsa.pub

qm start $VM_ID

echo "VM $VM_ID is created and started."