
# Confidential Computing Deployment (Azure DCsv5 / AMD SEV-SNP)
- Use DCsv5 VM nodes for AKS, schedule sensitive pods with `nodeSelector: agentpool: dcsv5`.
- Enforce attestation before processing PII/audio.
- Store attestation evidence in audit logs.
