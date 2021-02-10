# Deployment Project

This project contains deployment artefacts.

[![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https%3A%2F%2Fgithub.com%2Fpacodelacruz%2Fobservability-pubsub-functions%2Fblob%2Fmain%2Finfra%2Fazuredeploy.jsonc)
[![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg?sanitize=true)](http://armviz.io/#/?load=https%3A%2F%2Fgithub.com%2Fpacodelacruz%2Fobservability-pubsub-functions%2Fblob%2Fmain%2Finfra%2Fazuredeploy.jsonc)

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
