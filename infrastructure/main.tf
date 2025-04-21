resource "azurerm_resource_group" "group" {
  name     = "rg-${var.environment}-${var.service_name}"
  location = var.location
}

resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "la-${var.environment}-${var.service_name}"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  retention_in_days   = 30
}

resource "azurerm_application_insights" "appinsights" {
  name                = "ai-${var.environment}-${var.service_name}"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  application_type    = "web"
   workspace_id       = azurerm_log_analytics_workspace.log_analytics_workspace.id
}

resource "azurerm_service_plan" "plan" {
  name                = "asp-${var.environment}-${var.service_name}"
  resource_group_name = azurerm_resource_group.group.name
  location            = azurerm_resource_group.group.location
  os_type             = "Linux"
  sku_name            = "B1"
}

resource "azurerm_linux_web_app" "api" {
  name                = "api-${var.environment}-${var.service_name}"
  resource_group_name = azurerm_resource_group.group.name
  service_plan_id     = azurerm_service_plan.plan.id
  location            = azurerm_resource_group.group.location 
    
  site_config {
     application_stack {
      dotnet_version = "8.0"
    }
  }

  app_settings = {
      APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.appinsights.instrumentation_key
      ApiKey                         = var.api_key,
      StorageAccountConnectionString  = azurerm_storage_account.storage.primary_connection_string      
  }
}

resource "azurerm_user_assigned_identity" "github_identity" {
  name                = "mi-${var.environment}-${var.service_name}"
  resource_group_name = azurerm_resource_group.group.name
  location            = azurerm_resource_group.group.location  
}

# add a role assignment to the managed identity, with the role website contributor and the resource
# being the app service previously created 
resource "azurerm_role_assignment" "role_assignment" {
  scope                = azurerm_linux_web_app.api.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_user_assigned_identity.github_identity.principal_id
}

resource "azurerm_federated_identity_credential" "github_federated_identity" {
  name                = "github-federated"
  resource_group_name = azurerm_resource_group.group.name
  audience            = ["api://AzureADTokenExchange"]
  issuer              = "https://token.actions.githubusercontent.com"
  parent_id           = azurerm_user_assigned_identity.github_identity.id
  subject             = "repo:gsej/me-tracker:environment:Production"
}


resource "azurerm_storage_account" "storage" {
  name                     = replace("st${var.environment}${var.service_name}", "-", "")
  resource_group_name      = azurerm_resource_group.group.name
  location                 = azurerm_resource_group.group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"  
  
  min_tls_version         = "TLS1_2"
}