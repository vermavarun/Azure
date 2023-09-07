resource "azurerm_service_plan" "vverma_app_service_plan" {
  name                = ""
  resource_group_name = ""
  location            = "West Europe"
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_linux_function_app" "vverma_linux_function_app" {
  name                       = ""
  resource_group_name        = ""
  location                   = "West Europe"
  https_only                 = true
  storage_account_name       = ""
  storage_account_access_key = ""
  service_plan_id            = azurerm_service_plan.vverma_app_service_plan.id

  site_config {
  }
}
