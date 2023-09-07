resource "azurerm_service_plan" "vverma_app_service_plan" {
  name                = "invvgg-app-service-plan"
  resource_group_name = "sbox-invvgg-dev-rg"
  location            = "West Europe"
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_linux_function_app" "vverma_linux_function_app" {
  name                       = "vverma-linux-function-app"
  resource_group_name        = "sbox-invvgg-dev-rg"
  location                   = "West Europe"
  https_only                 = true
  storage_account_name       = "invvgg14385sa"
  storage_account_access_key = "aZQDxxCoZfwyDTs7F9eWQn//p8nNE1VzaHO0XaDVO8mMNzxIsvTq2KdHmUK6yN8sr7hutB5U7d8x+ASt2mFLcA=="
  service_plan_id            = azurerm_service_plan.vverma_app_service_plan.id

  site_config {
  }
} 
