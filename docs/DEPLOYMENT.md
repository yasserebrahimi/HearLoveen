# HearLoveen Deployment Guide

Complete guide for deploying HearLoveen platform to production.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Infrastructure Setup](#infrastructure-setup)
3. [Database Migration](#database-migration)
4. [Service Deployment](#service-deployment)
5. [Monitoring Setup](#monitoring-setup)
6. [Post-Deployment Verification](#post-deployment-verification)

---

## Prerequisites

### Required Tools
- Azure CLI (`az`) v2.50+
- kubectl v1.28+
- Helm v3.12+
- Terraform v1.5+
- Docker v24.0+
- .NET SDK 8.0+

### Required Access
- Azure subscription with Contributor access
- Container Registry (ACR) push permissions
- AKS cluster admin access
- GitHub repository access

---

## Infrastructure Setup

### 1. Azure Resources (Terraform)

```bash
cd infrastructure/terraform

# Initialize Terraform
terraform init

# Review plan
terraform plan -out=tfplan

# Apply infrastructure
terraform apply tfplan
```

**Resources Created:**
- AKS Cluster (3-10 nodes)
- PostgreSQL Flexible Server (14)
- Redis Cache (Premium)
- Storage Account (GRS)
- Virtual Network + Subnets
- Network Security Groups
- IoT Hub
- Key Vault
- Application Insights

**Estimated Cost:** €500-800/month

### 2. Configure Environment Variables

```bash
# Copy template
cp .env.example .env

# Edit with production values
nano .env
```

**Required Variables:**
```env
POSTGRES_PASSWORD=<secure-password>
AZURE_STORAGE_CONNECTION_STRING=<storage-connection>
AZURE_SERVICE_BUS_CONNECTION_STRING=<servicebus-connection>
AZURE_AD_B2C_TENANT_ID=<tenant-id>
AZURE_AD_B2C_CLIENT_ID=<client-id>
GRAFANA_ADMIN_PASSWORD=<secure-password>
```

### 3. Azure Key Vault Setup

```bash
# Store secrets in Key Vault
az keyvault secret set --vault-name hearloveen-kv \
  --name "PostgresPassword" --value "<password>"

az keyvault secret set --vault-name hearloveen-kv \
  --name "StorageConnectionString" --value "<connection-string>"

az keyvault secret set --vault-name hearloveen-kv \
  --name "ServiceBusConnectionString" --value "<connection-string>"
```

---

## Database Migration

### 1. Run EF Core Migrations

```bash
# Set connection string
export ConnectionStrings__Postgres="Host=<postgres-fqdn>;Database=hearloveen;Username=dbadmin;Password=<password>"

# Run migrations
dotnet ef database update \
  --project src/infrastructure/HearLoveen.Infrastructure \
  --startup-project src/api/HearLoveen.Api \
  --connection "$ConnectionStrings__Postgres"
```

### 2. Verify Database Schema

```bash
# Connect to PostgreSQL
psql -h <postgres-fqdn> -U dbadmin -d hearloveen

# List tables
\dt

# Expected tables:
# - Users
# - Children
# - AudioSubmissions
# - FeedbackReports
# - FeatureVectors
# - PhonemeRatings
# - ChildCurricula
# - TherapistAssignments
# - Consents
```

---

## Service Deployment

### 1. Build Docker Images

```bash
# Login to ACR
az acr login --name hearloveen

# Build and push API Gateway
docker build -f src/ApiGateway/Dockerfile -t hearloveen.azurecr.io/apigateway:latest .
docker push hearloveen.azurecr.io/apigateway:latest

# Build and push AudioService
docker build -f src/AudioService/Dockerfile -t hearloveen.azurecr.io/audioservice:latest .
docker push hearloveen.azurecr.io/audioservice:latest

# Build and push AnalysisService
docker build -f src/AnalysisService/Dockerfile -t hearloveen.azurecr.io/analysisservice:latest .
docker push hearloveen.azurecr.io/analysisservice:latest

# Build and push UserService
docker build -f src/UserService/Dockerfile -t hearloveen.azurecr.io/userservice:latest .
docker push hearloveen.azurecr.io/userservice:latest

# Build and push NotificationService
docker build -f src/NotificationService/Dockerfile -t hearloveen.azurecr.io/notificationservice:latest .
docker push hearloveen.azurecr.io/notificationservice:latest

# Build and push IoTService
docker build -f src/IoTService/Dockerfile -t hearloveen.azurecr.io/iotservice:latest .
docker push hearloveen.azurecr.io/iotservice:latest

# Build and push ML Worker
docker build -f ml-platform/inference/api/Dockerfile -t hearloveen.azurecr.io/ml-worker:latest ml-platform/inference/api
docker push hearloveen.azurecr.io/ml-worker:latest
```

### 2. Deploy with Helm

```bash
# Get AKS credentials
az aks get-credentials --resource-group hearloveen-rg --name hearloveen-aks

# Create namespace
kubectl create namespace production

# Deploy with Helm
helm upgrade --install hearloveen ./deploy/k8s/helm/hear \
  --namespace production \
  --set image.repository=hearloveen.azurecr.io \
  --set image.tag=latest \
  --set postgres.host=<postgres-fqdn> \
  --set postgres.password=<postgres-password> \
  --set redis.host=<redis-hostname> \
  --set servicebus.connectionString=<servicebus-connection> \
  --set storage.connectionString=<storage-connection> \
  --wait \
  --timeout 10m
```

### 3. Verify Deployment

```bash
# Check pods
kubectl get pods -n production

# Expected output:
# NAME                                  READY   STATUS    RESTARTS   AGE
# apigateway-xxxxxxxxxx-xxxxx          1/1     Running   0          2m
# audioservice-xxxxxxxxxx-xxxxx        1/1     Running   0          2m
# analysisservice-xxxxxxxxxx-xxxxx     1/1     Running   0          2m
# userservice-xxxxxxxxxx-xxxxx         1/1     Running   0          2m
# notificationservice-xxxxxxxxxx-xxxxx 1/1     Running   0          2m
# iotservice-xxxxxxxxxx-xxxxx          1/1     Running   0          2m
# ml-worker-xxxxxxxxxx-xxxxx           1/1     Running   0          2m

# Check logs
kubectl logs -n production -l app=apigateway --tail=50

# Check services
kubectl get svc -n production
```

---

## Monitoring Setup

### 1. Deploy Prometheus

```bash
# Install Prometheus Operator
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm install prometheus prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --set prometheus.prometheusSpec.serviceMonitorSelectorNilUsesHelmValues=false \
  --values deploy/observability/prometheus/values.yaml
```

### 2. Configure Alerts

```bash
# Apply alert rules
kubectl apply -f deploy/observability/prometheus/alerts/api-alerts.yml -n monitoring
kubectl apply -f deploy/observability/prometheus/alerts/business-alerts.yml -n monitoring
```

### 3. Deploy Grafana

```bash
# Grafana is included with Prometheus Operator
# Get admin password
kubectl get secret -n monitoring prometheus-grafana -o jsonpath="{.data.admin-password}" | base64 --decode

# Port forward to access
kubectl port-forward -n monitoring svc/prometheus-grafana 3000:80

# Import dashboards
# - Open http://localhost:3000
# - Login with admin/<password>
# - Import: deploy/observability/grafana/dashboards/*.json
```

### 4. Configure Alertmanager

```bash
# Edit alertmanager config
kubectl edit secret -n monitoring alertmanager-prometheus-kube-prometheus-alertmanager

# Add Slack/PagerDuty integration
# See: deploy/observability/alertmanager/config.yml
```

---

## Post-Deployment Verification

### 1. Health Checks

```bash
# Check all services
for service in apigateway audioservice analysisservice userservice notificationservice iotservice; do
  echo "Checking $service..."
  kubectl exec -n production -it $(kubectl get pod -n production -l app=$service -o jsonpath='{.items[0].metadata.name}') -- curl -s http://localhost/health
done
```

### 2. Smoke Tests

```bash
# Run k6 smoke test
cd tests/perf/k6
k6 run --env BASE_URL=https://api.hearloveen.com api-load-test.js
```

### 3. Database Verification

```bash
# Check data
psql -h <postgres-fqdn> -U dbadmin -d hearloveen -c "SELECT COUNT(*) FROM \"Users\";"
psql -h <postgres-fqdn> -U dbadmin -d hearloveen -c "SELECT COUNT(*) FROM \"AudioSubmissions\";"
```

### 4. Monitoring Verification

```bash
# Check Prometheus targets
kubectl port-forward -n monitoring svc/prometheus-kube-prometheus-prometheus 9090:9090
# Open: http://localhost:9090/targets

# Check Grafana dashboards
kubectl port-forward -n monitoring svc/prometheus-grafana 3000:80
# Open: http://localhost:3000
```

---

## Rollback Procedure

If deployment fails:

```bash
# Rollback Helm release
helm rollback hearloveen -n production

# Check rollback status
helm history hearloveen -n production

# Verify pods
kubectl get pods -n production
```

---

## Scaling

### Horizontal Pod Autoscaling

```bash
# Already configured via KEDA
# Check HPA status
kubectl get hpa -n production

# Manual scaling
kubectl scale deployment apigateway -n production --replicas=5
```

### Node Autoscaling

```bash
# Already configured via Karpenter
# Check node status
kubectl get nodes
```

---

## Troubleshooting

### Pod not starting

```bash
# Describe pod
kubectl describe pod <pod-name> -n production

# Check events
kubectl get events -n production --sort-by='.lastTimestamp'

# Check logs
kubectl logs <pod-name> -n production --previous
```

### Database connection issues

```bash
# Test connection
kubectl run -it --rm psql-test --image=postgres:14 --restart=Never -- \
  psql -h <postgres-fqdn> -U dbadmin -d hearloveen
```

### Service Bus issues

```bash
# Check Service Bus metrics in Azure Portal
# Verify connection string in secrets

kubectl get secret -n production hearloveen-secrets -o yaml
```

---

## Security Checklist

- [ ] All secrets stored in Azure Key Vault
- [ ] NSG rules configured
- [ ] TLS certificates installed
- [ ] RBAC properly configured
- [ ] Network policies applied
- [ ] Pod security policies enforced
- [ ] Backup strategy tested
- [ ] Disaster recovery plan documented

---

## Support

For issues or questions:
- GitHub Issues: https://github.com/yasserebrahimi/HearLoveen/issues
- Documentation: /docs/
- SRE Runbooks: /docs/sre/

---

**Last Updated:** 2025-01-04
**Version:** 1.0.0
