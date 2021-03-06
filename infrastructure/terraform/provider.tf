terraform {
  required_version = ">= 0.13.0"
}

provider "azurerm" {
  subscription_id = ""
  tenant_id       = ""
  version         = ">=2.24.0"
  features {}
}

terraform {
  backend "azurerm" {
    resource_group_name  = "rg-workshop"
    storage_account_name = "stotfstateci"
    container_name       = "state"
    key                  = "ci/terraform.tfstate"
  }
}
