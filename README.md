# AzureFunctionExample

Basic example showcasing an azure function HTTP trigger with dotnet 8 using the isolated worker model.


### Setup

* create local development file `Function/local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```


### Useful Links

* HTTP Trigger in isolated worker model: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cfunctionsv2&pivots=programming-language-csharp

* Setting up OpenAPI docs generation: https://github.com/Azure/azure-functions-openapi-extension/blob/main/docs/enable-open-api-endpoints-out-of-proc.md

* Azure Storage Explorer: https://azure.microsoft.com/en-us/products/storage/storage-explorer/ (On Linux flatpak can be used: https://flathub.org/apps/com.microsoft.AzureStorageExplorer)
