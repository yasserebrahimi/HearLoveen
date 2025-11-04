param location string = resourceGroup().location
param namePrefix string = 'hear'

resource sa 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${namePrefix}stor'
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
}

resource kv 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: '${namePrefix}-kv'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    accessPolicies: []
    enabledForDeployment: true
    enablePurgeProtection: true
  }
}

resource sb 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${namePrefix}-sb'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
}

resource sbq 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: '${sb.name}/audio-submitted'
  properties: {
    enablePartitioning: true
    maxSizeInMegabytes: 5120
  }
}

output storageAccountName string = sa.name
output keyVaultName string = kv.name
output serviceBusName string = sb.name
