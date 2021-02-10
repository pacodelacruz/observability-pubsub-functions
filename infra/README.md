# Deployment Project

This project contains deployment artefacts.

[![Deploy to Azure](https://docs.microsoft.com/en-us/azure/templates/media/deploy-to-azure.svg)](https://deploy.azure.com/?repository=https://github.com/pacodelacruz/serverless-request-bin-azure-functions/infra#/form/setup)

## CLI Snippets

To connect to Azure CLI and set the right context:

``` shell
az login
az account set --subscription <subscription id>
```

### ARM Template deployments

Example to deploy with a parameters file.

``` shell
az deployment group create --name CliDeployment --resource-group <resource group name> --template-file "azuredeploy.jsonc" --parameters "azuredeploy.parameters.jsonc"
```
