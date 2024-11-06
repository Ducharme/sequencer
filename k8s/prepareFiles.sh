#!/bin/sh

mkdir -p .tmp/k8s

GITHUB_BRANCH=https://raw.githubusercontent.com/Ducharme/sequencer-cdk/main
curl -s -LO --output-dir .tmp/k8s $GITHUB_BRANCH/k8s/adminwebportal-deployment.yml
curl -s -LO --output-dir .tmp/k8s $GITHUB_BRANCH/k8s/adminwebportal-service.yml
curl -s -LO --output-dir .tmp/k8s $GITHUB_BRANCH/k8s/processorwebservice-deployment.yml
curl -s -LO --output-dir .tmp/k8s $GITHUB_BRANCH/k8s/sequencerwebservice-deployment.yml
curl -s -LO --output-dir .tmp/k8s $GITHUB_BRANCH/k8s/sequencer-configmap-template.yml
curl -s -LO --output-dir .tmp $GITHUB_BRANCH/waitForCompletion.sh
curl -s -LO --output-dir .tmp $GITHUB_BRANCH/waitForContainer.sh

curl -s -LO --output-dir .tmp $GITHUB_BRANCH/datadog-values.yml
curl -s -LO --output-dir .tmp $GITHUB_BRANCH/setDatadogEnvVars.sh
curl -s -LO --output-dir .tmp $GITHUB_BRANCH/setupDatadog.sh


AWP_YML=.tmp/k8s/adminwebportal-deployment.yml
yq -y -i 'del(.spec.template.spec.serviceAccountName)' $AWP_YML
yq -y -i 'del(.spec.template.spec.containers[].env[] | select(.name == "REDIS_PASSWORD"))' $AWP_YML
yq -y -i 'del(.spec.template.spec.containers[].volumeMounts)' $AWP_YML
yq -y -i 'del(.spec.template.spec.volumes)' $AWP_YML
yq -y -i 'del(.spec.template.spec.affinity)' $AWP_YML

PWS_YML=.tmp/k8s/processorwebservice-deployment.yml
yq -y -i 'del(.spec.template.spec.serviceAccountName)' $PWS_YML
yq -y -i 'del(.spec.template.spec.containers[].env[] | select(.name == "REDIS_PASSWORD"))' $PWS_YML
yq -y -i 'del(.spec.template.spec.containers[].volumeMounts)' $PWS_YML
yq -y -i 'del(.spec.template.spec.volumes)' $PWS_YML
yq -y -i 'del(.spec.template.spec.affinity)' $PWS_YML

SWS_YML=.tmp/k8s/sequencerwebservice-deployment.yml
yq -y -i 'del(.spec.template.spec.serviceAccountName)' $SWS_YML
yq -y -i 'del(.spec.template.spec.containers[].env[] | select(.name == "REDIS_PASSWORD"))' $SWS_YML
yq -y -i 'del(.spec.template.spec.containers[].volumeMounts)' $SWS_YML
yq -y -i 'del(.spec.template.spec.volumes)' $SWS_YML
yq -y -i 'del(.spec.template.spec.affinity)' $SWS_YML

# Prepare Ingress
