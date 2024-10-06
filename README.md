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
## To add a unique id to the node name
```bash
# on worker node
vi /etc/init.d/k3s-agent

# find the line where the agent is started and append --with-node-id. For example:
# command_args="agent --with-node-id \
#     >>/var/log/k3s-agent.log 2>&1"

# restart the agent
rc-service k3s-agent restart
```