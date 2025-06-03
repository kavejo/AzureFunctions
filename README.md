# ACS Azure Functions

Sample code to demonstrate how to leverage Azure Functions to integrate with Azure Communication Services (ACS) Email as well as Exchange Server.

Currently this sample allows to send email messages via ACS either using REST API (.NET SDK) or SMTP, while it only connect via SMTP when targeting a mail server like Exchange Server 2019.
For what concerns authentication, sending via REST uses Managed Identities and Service Principals, while SMTP uses Basic Authentication.

The sample showcase also some basic integrations with Azure OpenAI (currently using the 04-mini model) to analyse the messages for Personal Identifiable Information (PII), Harmful Content, or to generate the actual content of the message.
When PII or Harmful Content are detected, the Azure Fucntion will prevent the send operation.

## Required Permissions

In order to be able to send emails via ACS, the Azure Function must hve a System Managed - Managed Identity as shown below.

![image](https://github.com/user-attachments/assets/e7f6cb52-ec05-435e-9e19-a16c55ee3c2b)

This Managed Identity needs to be granted **Communication and Email Service Owner** on the ACS resource in order to be able to send messages.
This is the same role that the App Registration requires when you implement the SMTP flow; the only difference resides on the fact that for REST this must be granted on the Managed Identity, while for SMTP this is granted to the Entra ID App Registration.

The screenshot below displays the permission that has been assinged.

![image](https://github.com/user-attachments/assets/3ed1f38e-83f6-45e5-8471-5a877954609a)

In order to be able to manage Suppression Lists (Unsubscribe function) the same permission must be set on the Email Communciation Service and the Domain.

The permission as set on the ECS is shown below.

![image](https://github.com/user-attachments/assets/c12afbaa-41f1-4d7f-9c36-3fbd73259636)

Simialrly, the screenshot below displays the permission set on the ECS Domain.

![image](https://github.com/user-attachments/assets/32a454c5-0d7f-47d1-9c8b-84d77b620338)

In order to leverage the Azure OpenAI capabilities, it also necessary to grant this Managed Identity permissions on the Azure AI Foundry resource.
The **Cognitive Services Language Reader** is required for the PII and Harmful Content analysis, while the **Cognitive Services OpenAI Contributor** is used for the body generation.

![image](https://github.com/user-attachments/assets/178af4de-2877-4ee8-8a58-8cb64ddb7b0f)

## Configuration

The configuration is primarily done via Environment Variables.

When building and testing this code in Visual Studio or Visual Studio Code, these can be supplied via the **local.settings.json** file.

In the actual Azure Function these are supplied via Environment Variables as shown in the consecutive screenshot.

![image](https://github.com/user-attachments/assets/11e83086-5638-48c6-a3bd-5c8dc561e031)

The following are the required configuration variables

| Variable Name          | Value                                                     | Used by                                                     | Notes                                                                                                                                                                                                                                                                                                                                                              |
|------------------------|-----------------------------------------------------------|-------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ACS_EMAIL_ENDPOINT     | https://YourACSSercice.communication.azure.com/           | SendMailViaREST                                             | This is the Endpoint of your Azure Communication Service _(from ACS > Keys > Endoint)_                                                                                                                                                                                                                                                                             |
| AZURE_OPENAI_ENDPOINT  | https://YourAIFoundryProject.cognitiveservices.azure.com/ | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH             | This is the Endpoint of your Azure AI Foundry Congnitive Service _(from AI Foundry > Keys and Endpoints > Document Translation)_                                                                                                                                                                                                                                   |
| AZURE_OPENAI_KEY       | string                                                    | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH             | This is the Key of your Azure AI Foundry Congnitive Service _(from AI Foundry > Keys and Endpoints > Key 1)_                                                                                                                                                                                                                                                       |
| AZURE_OPENAI_MODEL     | o4-mini                                                   | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH             | This is the Model Name you have set when you deployed one of the AI Models _(from AI Foundry > Models + Endpoints > Name)_                                                                                                                                                                                                                                         |
| ALLOWED_HOSTS          | host1,host2,hostN                                         | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH             | In order to provent calls from unauthorized hosts, some basic IP-checks hare performed. Only hosts in this list will be allowed to make calls to the Azure Function. Set the value to ANY, ALL, or * to allow any host.                                                                                                                                            |
| ACS_SMTP_ENDPOINT      | smtp.azurecomm.net                                        | SendMailViaSMTP                                             | The SMTP Endpoint for Azure Communication Services Email _(this is smtp.azurecomm.net for anyone)_                                                                                                                                                                                                                                                                 |
| ACS_SMTP_PORT          | 587                                                       | SendMailViaSMTP                                             | The SMTP Port for Azure Communication Services Email _(this can be set to either 25 or 587)_                                                                                                                                                                                                                                                                       |
| ACS_SMTP_USERNAME      | string                                                    | SendMailViaSMTP                                             | The SMTP Username for the Azure Communication Services Email _(Ref.: [Create credentials for Simple Mail Transfer Protocol (SMTP) authentication](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/send-email-smtp/smtp-authentication?tabs=built-in-role)_                                                                        |
| ACS_SMTP_APPID         | string                                                    | SendMailViaSMTP                                             | The Entra ID ClientID for the App registered for Azure Communication Services Email _(Ref.: [Register an application with Microsoft Entra ID and create a service principal](https://learn.microsoft.com/en-us/entra/identity-platform/howto-create-service-principal-portal#register-an-application-with-microsoft-entra-id-and-create-a-service-principal)_      |
| ACS_SMTP_CLIENTSECRET  | string                                                    | SendMailViaSMTP                                             | The Entra ID Client Secret for the App registered for Azure Communication Services Email _(Ref.: [Register an application with Microsoft Entra ID and create a service principal](https://learn.microsoft.com/en-us/entra/identity-platform/howto-create-service-principal-portal#register-an-application-with-microsoft-entra-id-and-create-a-service-principal)_ |
| ACS_SMTP_TENANTID      | string                                                    | SendMailViaSMTP                                             | The Entra ID Tenant ID for your tenant _(Ref.: [Register an application with Microsoft Entra ID and create a service principal](https://learn.microsoft.com/en-us/entra/identity-platform/howto-create-service-principal-portal#register-an-application-with-microsoft-entra-id-and-create-a-service-principal)_                                                   |
| ACS_SMTP_AUTHORITY     | https://login.microsoftonline.com/common/                 | SendMailViaSMTP                                             | The value "https://login.microsoftonline.com/common/" which work for all Office 365 customers; customers in special jursdictions might avail of a different authority URL                                                                                                                                                                                          |
| EXCH_SMTP_ENDPOINT     | smtp.azurecomm.net                                        | SendMailViaEXCH                                             | The SMTP Endpoint for Azure Communication Services Email _(this is smtp.azurecomm.net for anyone)_ or the FQDN of your local Mail Server                                                                                                                                                                                                                           |
| EXCH_SMTP_PORT         | 587                                                       | SendMailViaEXCH                                             | The SMTP Port for Azure Communication Services Email _(this can be set to either 25 or 587)_, in case of local your local mail server this can also be set to 465 in most cases                                                                                                                                                                                    |
| EXCH_SMTP_USERNAME     | string                                                    | SendMailViaEXCH                                             | The SMTP Username for the Azure Communication Services Email _(Ref.: [Create credentials for Simple Mail Transfer Protocol (SMTP) authentication](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/send-email-smtp/smtp-authentication?tabs=built-in-role)_, or your local user username, in case of a local mail server           |
| EXCH_SMTP_PASSWORD     | string                                                    | SendMailViaEXCH                                             | The SMTP Password for the Azure Communication Services Email _(Ref.: [Create credentials for Simple Mail Transfer Protocol (SMTP) authentication](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/send-email-smtp/smtp-authentication?tabs=built-in-role)_, or your local user password, in case of a local mail server           |
| DEFAULT_SENDER         | sender@yourDomain                                         | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH             | The default sending address which will be set for the messages that are sent when a custom sender is not provided via Query String or POST Body. For EXCH this shall match EXCH_SMTP_USERNAME, for ACS this will be one of the Sender Addresses configured for the ecs Domain                                                                                      |
| DEFAULT_RECIPIENT      | recipient@recipientDomain                                 | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH             | The default sending address which will be set for the messages that are sent when a custom recipient is not provided via Query String or POST Body                                                                                                                                                                                                                 |
| UNSUB_SUBSCRIPTION     | string                                                    | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH,Unsubscribe | The Subscription ID where the Azure Communication Services is hosted                                                                                                                                                                                                                                                                                               |
| UNSUB_RESOURCE_GROUP   | string                                                    | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH,Unsubscribe | The Resource Group where the Azure Communication Services is hosted                                                                                                                                                                                                                                                                                                |
| UNSUB_EMAIL_SERVICE    | string                                                    | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH,Unsubscribe | The name of the Azure Communication Services resource                                                                                                                                                                                                                                                                                                              |
| UNSUB_DOMAIN           | string                                                    | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH,Unsubscribe | The domain from which emails are sent and where eventually the recipient willing to unsubscribe has to be added to Suppression List                                                                                                                                                                                                                                |
| UNSUB_SUPPRESSION_LIST | string                                                    | SendMailViaREST,SendMailViaSMTP,SendMailViaEXCH,Unsubscribe | The name of the Suppression List where the recipient will be added to                                                                                                                                                                                                                                                                                              |

## Note about the capabilities

For the sake of this demo, the Azure Fucntions sends the email to a single recipient (To:) and the message is sent from the configured sender which can have different P1 (MAIL FROM:) and P2 (From:) SMTP Addresses.

ACS, as well as Microsoft Exchange can clearly handle multiple recipients in different mix between To:, Cc: and Bcc: as well as handle senders with mismathcing P1 and P2, however for this sample we've opted for targetiong just one recipient in To:.

In the Send* fucntions, any of the parameter can be passed either via HTTP GET (through Query Parameters) or via HTTP POST (via Request Body).
When a HTTP GET is issued, no body will be considered, while when a GTTP POST is issed any Quuery Parameter will be ignore.
Values that are not provided will be set to their default value.

The following is the list of avaialble paramters:
```json
{ 
  "Type": 1,
  "From": "noreply@toniolo.cloud",
  "ReplyTo": "noreply@toniolo.cloud",
  "To": "totoni@microsoft.com",
  "Subject":"This is my custom Subject",
  "TextBody":"This is my custom Subject"
  "HtmlBody":"This is my custom Subject"
  "CustomContent":"This is my custom subject"
}
```

This is instead the possible values for "Type" which is an Integer with value between 0 and 6.
```csharp
  SendMail = 0,
  SendMailWithPIIScan = 1,
  SendWithPIIRedacted = 2,
  SendMailWithHarmfulContentScan = 3,
  SendMailWithPIIAndHarmfulContentScan = 4,
  SendMailWithGeneragedBody = 5
```

## Difference between the fucntions

**SendMailViaREST**: This function will send the email message via Azure Communication Services using the REST API. It will use the Managed Identity of the Azure Function to authenticate against ACS and send the message.
**SendMailViaSMTP**: This function will send the email message via Azure Communication Services using SMTP. It will use the Entra ID App Registration to authenticate against ACS and send the message (leveraging OAuth).
**SendMailViaEXCH**: This function will send the email message via SMTP to either Azure Communication Services or any other SMTP server (incl. Microsoft Exchange). It will use Basic Authentication to authenticate against the SMTP server and send the message.

## Operation

To call these APIs, it is possible to either run the Visual Studio project, or to deploy the Fucntions to Azure Functions.
In the latter case, the deployment can be done starting from this repository, through a CD/CI pipeline built on top of GitHub Actions, or from Visual Studio through a PubXML file.

Generally speaking, **CustomContent** is what will be inserted in the **TextBody** and **HtmlBody** when the message is sent out (Type between 0 and 4), and which will be analuysed when the Type value requires it.
Instead, the **CustomContent** will be used to generate the text/paragrpah via Azure OpenAI when Type = 5, and then the generated content will be inserted into the message.

The subejct has a default value of "AzureFunctions:EmailMessage Test performed on yyyy-mm-dd at hh:MM:ss" when none is specified.

## Sample message with all default values

![image](https://github.com/user-attachments/assets/4def3e92-6f5b-4b1e-8911-eb9436e56e4e)
