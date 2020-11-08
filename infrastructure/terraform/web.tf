data "azurerm_resource_group" "resource_group" {
  name = "rg-workshop"
}

data "azurerm_subscription" "main" {
}

resource "azurerm_app_service_plan" "webapps" {
  name                = "asp-workshop-ci"
  location            = data.azurerm_resource_group.resource_group.location
  resource_group_name = data.azurerm_resource_group.resource_group.name

  #NOTE: When creating a Linux App Service Plan, the reserved field must be set to true, and when creating a Windows/app App Service Plan the reserved field must be set to false.
  kind     = "Windows"
  reserved = false

  sku {
    tier = "Standard"
    size = "S1"
  }
}


resource "azurerm_app_service" "web" {
  name                = "api-workshop-ci"
  location            = data.azurerm_resource_group.resource_group.location
  resource_group_name = data.azurerm_resource_group.resource_group.name
  app_service_plan_id = azurerm_app_service_plan.webapps.id
  https_only          = true

  site_config {
    dotnet_framework_version  = "v4.0"
    always_on                 = true
    use_32_bit_worker_process = true
    http2_enabled             = true
  }

  app_settings = {
    WEBSITE_NODE_DEFAULT_VERSION = "6.9.1"
    WEBSITE_OWNER_NAME           = data.azurerm_subscription.main.id
  }

  #NOTE: When type is set to SystemAssigned, The assigned principal_id and tenant_id can be retrieved after the App Service has been created. More details are available below.
  identity {
    type = "SystemAssigned"
  }

  tags = local.tags
}

data "azurerm_virtual_network" "vnet" {
  name                = "vnet"
  resource_group_name = data.azurerm_resource_group.resource_group.name
}

resource "azurerm_subnet" "subnet_appservices" {
  name                 = "subnet-appservices"
  resource_group_name  = data.azurerm_resource_group.resource_group.name
  virtual_network_name = data.azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.3.0/24"]

  delegation {
    name = "web-delegation"

    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }

}

resource "azurerm_app_service_virtual_network_swift_connection" "vnet_integration" {
  app_service_id = azurerm_app_service.web.id
  subnet_id      = azurerm_subnet.subnet_appservices.id
}
