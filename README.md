# K8s

## Create a VM (based on alpine)
```bash
wget https://raw.githubusercontent.com/saeedmaghdam/k8s/refs/heads/main/create-alpine-machine.sh
chmod +x create-alpine-machine.sh
sh ./create-alpine-machine.sh
```

## Install k3s/kubeadm
```bash
wget https://raw.githubusercontent.com/saeedmaghdam/k8s/refs/heads/main/start.sh
chmod +x start.sh
sh ./start.sh

sh 1-setup-alpine.sh

reboot

sh 2-config-alpine.sh

# if you installed k3s, you can get the config file using the following command
sh 3-get-config.sh
```

## Steps to create a cluster with multiple worker nodes
```bash
# on master node
kubeadm token create --print-join-command

# on worker node
kubekubeadm join ...
```
