@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

var resourceToken = uniqueString(resourceGroup().id)

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-${resourceToken}'
  location: location
  tags: tags
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: replace('acr-${resourceToken}', '-', '')
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
  tags: tags
}

resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  scope: containerRegistry
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'cae-${resourceToken}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
  tags: tags
}

resource statestore 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: 'statestore'
  parent: containerAppEnvironment
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    metadata: [

      {
        name: 'redisHost'

        value: '${daprStore.name}:6379'
}

      {
        name: 'redisPassword'

        secretRef: 'password'
}
]
    secrets: [

      {
        name: 'password'
        value: substring(daprStore.listSecrets().value[0].value, 12, 128)
      }
]
  }
}

resource daprStore 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: 'daprstore'
  location: location
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      service: {
        type: 'redis'
      }
    }
    template: {
      containers: [
        {
          image: 'redis'
          name: 'redis'
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  tags: union(tags, {'aspire-resource-name': 'daprStore'})
}

resource addapp 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: 'addapp'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6000
        transport: 'http'
        allowInsecure: true
      }
      registries: [
        {
          server: '${containerRegistry.name}.azurecr.io'
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.name}.azurecr.io/addapp:latest'
          name: 'addapp'
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  tags: union(tags, {'aspire-resource-name': 'addapp'})
}

resource divideapp 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: 'divideapp'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 4000
        transport: 'http'
        allowInsecure: true
      }
      registries: [
        {
          server: '${containerRegistry.name}.azurecr.io'
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.name}.azurecr.io/divideapp:latest'
          name: 'divideapp'
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  tags: union(tags, {'aspire-resource-name': 'divideapp'})
}

resource multiplyapp 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: 'multiplyapp'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5001
        transport: 'http'
        allowInsecure: true
      }
      registries: [
        {
          server: '${containerRegistry.name}.azurecr.io'
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.name}.azurecr.io/multiplyapp:latest'
          name: 'multiplyapp'
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  tags: union(tags, {'aspire-resource-name': 'multiplyapp'})
}

output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output MANAGED_IDENTITY_NAME string = managedIdentity.name
output MANAGED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.name
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.properties.loginServer
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = managedIdentity.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.properties.defaultDomain
