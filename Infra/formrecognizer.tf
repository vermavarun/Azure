resource "azurerm_cognitive_account" "invvggformrecognizer" {
  name                = "invvggformrecognizer"
  location            = "West Europe"
  resource_group_name = "sbox-invvgg-dev-rg"
  kind                = "FormRecognizer"

  sku_name = "S0"

}