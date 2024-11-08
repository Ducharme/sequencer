
Install kind local kubernetes cluster [link](https://kind.sigs.k8s.io/docs/user/quick-start/#installation)

```
[ $(uname -m) = x86_64 ] && curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.24.0/kind-linux-amd64
chmod +x ./kind
sudo mv ./kind /usr/local/bin/kind
```

Configure kind
```
sudo kind create cluster --name local-sequencer
kind export kubeconfig --name local-sequencer

kind get clusters
kubectl config get-contexts

# Switch to kind context (if cluster name is "kind")
#kubectl config use-context kind-local-sequencer
```

Follow steps to [Deploy Datadog monitoring](https://github.com/Ducharme/sequencer-cdk/tree/main?tab=readme-ov-file#seventh-step-deploy-datadog-monitoring) or copy files .datadog from root folder sequencer-cdk if already setup. Configuration file should be .tmp/.datadog
```
sh k8s/prepareFiles.sh
cp ../sequencer-cdk/.datadog .tmp/
```

Deploy redis
```
kubectl apply -f k8s/redis.yml
POD_NAME=$(kubectl get po -o name | grep local-redis | cut -d'/' -f2)
kubectl exec -it $POD_NAME -- redis-cli
PING
# Should receive PONG as output
```

Deploy app
```
sh k8s/generateConfigMap.sh
kubectl apply -f .tmp/k8s/processorwebservice-deployment.yml
kubectl apply -f .tmp/k8s/sequencerwebservice-deployment.yml
kubectl apply -f .tmp/k8s/adminwebportal-deployment.yml
kubectl apply -f .tmp/k8s/adminwebportal-service.yml
```

Install MetalLB for ingress
```
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/v0.13.7/config/manifests/metallb-native.yaml
kubectl apply -f k8s/adminwebportal-loadbalancer.yml
AWP_IP=$(kubectl get svc adminwebportal-lb -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
kubectl apply -f k8s/adminwebportal-loadbalancer.yml
```

Import local images to the cluster
```
kind load docker-image adminwebportal:latest --name local-sequencer
kind load docker-image claudeducharme/adminwebportal:0.0.38-aspnet8.0.8-bookworm-slim-datadog3.3.1 --name local-sequencer
kubectl rollout restart deployment adminwebportal
```

Restart kind cluster
```
docker start local-sequencer-control-plane
```