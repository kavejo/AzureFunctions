using Azure;
using Azure.AI.ContentSafety;
using Azure.AI.OpenAI;
using Azure.AI.TextAnalytics;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using MimeKit;
using OpenAI.Assistants;
using OpenAI.Chat;

namespace AzureFunctions
{
    public class EmailMessageRequest
    {
        public EmailMessageRequestType Type { get; set; } = EmailMessageRequestType.SendMail;
        public string From { get; set; } = Environment.GetEnvironmentVariable("DEFAULT_SENDER");
        public string ReplyTo { get; set; } = Environment.GetEnvironmentVariable("DEFAULT_SENDER");
        public string To { get; set; } = Environment.GetEnvironmentVariable("DEFAULT_RECIPIENT");
        public string Subject { get; set; } = String.Format("AzureFunctions:EmailMessage Test performed on {0:D2}-{1:D2}-{2:D2} at {3:D2}:{4:D2}:{5:D2}", 
            DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day, 
            DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, DateTime.Now.TimeOfDay.Seconds
        );
        public string TextBody { get; set; } = @"Hello! \r\n This is a test e-mail message sent to you via Azure Communication Services. \r\n _PLACEHOLDER_ \r\n This message has been sent to you as a test. \r\n Please feel free to ignore it. \r\n For more information: https://aka.ms/totoni.\r\n To unsubscribe: _UNSUBSCRIBE_LINK_";
        public string HtmlBody { get; set; } = @"<!doctype html><html lang=""en""><head><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8""><title>Test email sent via Azure Communication Services Email</title><style media=""all"" type=""text/css"">@media all {.btn-primary table td:hover {background-color: #ec0867 !important;}.btn-primary a:hover {background-color: #ec0867 !important;border-color: #ec0867 !important;}}@media only screen and (max-width: 640px) {.main p,.main td,.main span {font-size: 16px !important;}.wrapper {padding: 8px !important;}.content {padding: 0 !important;}.container {padding: 0 !important;padding-top: 8px !important;width: 100% !important;}.main {border-left-width: 0 !important;border-radius: 0 !important;border-right-width: 0 !important;}.btn table {max-width: 100% !important;width: 100% !important;}.btn a {font-size: 16px !important;max-width: 100% !important;width: 100% !important;}}@media all {.ExternalClass {width: 100%;}.ExternalClass,.ExternalClass p,.ExternalClass span,.ExternalClass font,.ExternalClass td,.ExternalClass div {line-height: 100%;}.apple-link a {color: inherit !important;font-family: inherit !important;font-size: inherit !important;font-weight: inherit !important;line-height: inherit !important;text-decoration: none !important;}#MessageViewBody a {color: inherit;text-decoration: none;font-size: inherit;font-family: inherit;font-weight: inherit;line-height: inherit;}}</style></head><body style=""font-family: Helvetica, sans-serif; -webkit-font-smoothing: antialiased; font-size: 16px; line-height: 1.3; -ms-text-size-adjust: 100%; -webkit-text-size-adjust: 100%; background-color: #f4f5f6; margin: 0; padding: 0;""><table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""body"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #f4f5f6; width: 100%;"" width=""100%"" bgcolor=""#f4f5f6""><tr><td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top;"" valign=""top"">&nbsp;</td><td class=""container"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; max-width: 600px; padding: 0; padding-top: 24px; width: 600px; margin: 0 auto;"" width=""600"" valign=""top""><div class=""content"" style=""box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;""><!-- START CENTERED WHITE CONTAINER --><span class=""preheader"" style=""color: transparent; display: none; height: 0; max-height: 0; max-width: 0; opacity: 0; overflow: hidden; mso-hide: all; visibility: hidden; width: 0;"">This is the demo e-mail message preview. Feel free to ignore it.</span><table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""main"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; background: #ffffff; border: 1px solid #eaebed; border-radius: 16px; width: 100%;"" width=""100%""> <!-- START MAIN CONTENT AREA --> <tr> <td class=""wrapper"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; box-sizing: border-box; padding: 24px;"" valign=""top""> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">Hello!</p> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">This is a test e-mail message sent to you via Azure Communication Services.</p> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">_PLACEHOLDER_</p> <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""btn btn-primary"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; box-sizing: border-box; width: 100%; min-width: 100%;"" width=""100%""> <tbody> <tr> <td align=""left"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; padding-bottom: 16px;"" valign=""top""> <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: auto;""> <tbody> <tr> <td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; border-radius: 4px; text-align: center; background-color: #0867ec;"" valign=""top"" align=""center"" bgcolor=""#0867ec""> <a href=""https://aka.ms/totoni"" target=""_blank"" style=""border: solid 2px #0867ec; border-radius: 4px; box-sizing: border-box; cursor: pointer; display: inline-block; font-size: 16px; font-weight: bold; margin: 0; padding: 12px 24px; text-decoration: none; text-transform: capitalize; background-color: #0867ec; border-color: #0867ec; color: #ffffff;"">View Sender Details!</a> </td> </tr> </tbody> </table> </td> </tr> </tbody> </table> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">This message has been sent to you as a test.</p> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">Please feel free to ignore it.</p> </td> </tr> <!-- END MAIN CONTENT AREA --> </table><!-- START FOOTER --><div class=""footer"" style=""clear: both; padding-top: 24px; text-align: center; width: 100%;""> <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;"" width=""100%""> <tr> <td class=""content-block"" style=""font-family: Helvetica, sans-serif; vertical-align: top; color: #9a9ea6; font-size: 16px; text-align: center;"" valign=""top"" align=""center""> <span class=""apple-link"" style=""color: #9a9ea6; font-size: 16px; text-align: center;""><a href=""_UNSUBSCRIBE_LINK_"">Unsubscribe</a></span> </td> </tr> </table></div><!-- END FOOTER --><!-- END CENTERED WHITE CONTAINER --></div></td><td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top;"" valign=""top"">&nbsp;</td></tr></table></body></html>";
        public string CustomContent { get; set; } = String.Empty;

        public bool ProcessContent(ILogger logger, Uri? languageEndpoint = null, AzureKeyCredential? languageKey = null, string? modelName = "") 
        {
            logger.LogInformation("Entering AzureFunctions:ProcessContent.");

            if (CustomContent.Length > 5000)
            {
                logger.LogError(String.Format("The content of CustomContent exceeds the 5,000 charachters. The current size is : {0}.", CustomContent.Length));
                return false;
            }

            // To be implemented: this method should be used to process the content of the message before sending, it based on the value of EmailMessageRequestType Type
            switch (Type)
                {
                case EmailMessageRequestType.SendMail:
                    logger.LogInformation("Processing content for SendMail request type.");
                    return true;
                case EmailMessageRequestType.SendMailWithPIIScan:
                    logger.LogInformation("Processing content for SendMailWithPIIScan request type.");
                    return !PIIPresent(logger, languageEndpoint, languageKey);
                case EmailMessageRequestType.SendWithPIIRedacted:
                    logger.LogInformation("Processing content for SendWithPIIRedacted request type.");
                    return RedactPIIIfPresent(logger, languageEndpoint, languageKey);
                case EmailMessageRequestType.SendMailWithHarmfulContentScan:
                    logger.LogInformation("Processing content for SendMailWithHarmfulContentScan request type.");
                    return !HarmfulContentPresent(logger, languageEndpoint, languageKey);
                case EmailMessageRequestType.SendMailWithPIIAndHarmfulContentScan:
                    logger.LogInformation("Processing content for SendMailWithPIIAndHarmfulContentScan request type.");
                    return !(PIIPresent(logger, languageEndpoint, languageKey) || HarmfulContentPresent(logger, languageEndpoint, languageKey));
                case EmailMessageRequestType.SendMailWithGeneragedBody:
                    logger.LogInformation("Processing content for SendMailWithGeneragedBody request type.");
                    return GenerateBodyContent(logger, languageEndpoint, languageKey, modelName);
                default:
                    logger.LogError("Unble to process message as the Type is invalid.");
                    return false;
            }
        }

        public EmailMessage RetrieveMessageForREST(ILogger logger, string unsubscribeKey = "#")
        {
            logger.LogInformation("Entering AzureFunctions:RetrieveMessageForREST.");

            logger.LogInformation("  Replacing _PLACEHOLDER_ with Custom Content.");
            TextBody = TextBody.Replace("_PLACEHOLDER_", CustomContent);
            HtmlBody = HtmlBody.Replace("_PLACEHOLDER_", CustomContent);

            logger.LogInformation("  Updating _UNSUBSCRIBE_LINK_ with generated unsubscribe key.");
            TextBody = TextBody.Replace("_UNSUBSCRIBE_LINK_", unsubscribeKey);
            HtmlBody = HtmlBody.Replace("_UNSUBSCRIBE_LINK_", unsubscribeKey);

            logger.LogInformation("  Generating message content.");
            EmailContent emailContent = new EmailContent(Subject);
            emailContent.PlainText = TextBody;
            emailContent.Html = HtmlBody;

            logger.LogInformation("  Generating message envelope.");
            EmailMessage emailMessage = new EmailMessage(From, To, emailContent);
            emailMessage.ReplyTo.Add(new EmailAddress(ReplyTo));

            logger.LogInformation("  Message has been generated.");
            return emailMessage;
        }

        public MimeMessage RetrieveMessageForSMTP(ILogger logger, string unsubscribeKey = "#")
        {
            logger.LogInformation("Entering AzureFunctions:RetrieveMessageForSMTP.");

            logger.LogInformation("  Replacing _PLACEHOLDER_ with Custom Content.");
            TextBody = TextBody.Replace("_PLACEHOLDER_", CustomContent);
            HtmlBody = HtmlBody.Replace("_PLACEHOLDER_", CustomContent);

            logger.LogInformation("  Updating _UNSUBSCRIBE_LINK_ with generated unsubscribe key.");
            TextBody = TextBody.Replace("_UNSUBSCRIBE_LINK_", unsubscribeKey);
            HtmlBody = HtmlBody.Replace("_UNSUBSCRIBE_LINK_", unsubscribeKey);

            logger.LogInformation("  Generating message envelope.");
            MimeMessage emailMessage = new MimeMessage();
            emailMessage.From.Add(MailboxAddress.Parse(From));
            emailMessage.Headers.Add("Reply-To", ReplyTo);
            emailMessage.To.Add(MailboxAddress.Parse(To));
            emailMessage.Subject = Subject;

            logger.LogInformation("  Generating message content.");
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.TextBody = TextBody;
            bodyBuilder.HtmlBody = HtmlBody;
            emailMessage.Body = bodyBuilder.ToMessageBody();

            logger.LogInformation("  Message has been generated.");
            return emailMessage;
        }

        private bool PIIPresent(ILogger logger, Uri? languageEndpoint = null, AzureKeyCredential? languageKey = null)
        {
            logger.LogInformation("Entering AzureFunctions:PIIPresent.");

            if (languageEndpoint == null || languageKey == null)
            {
                logger.LogError("Language endpoint and key are necessary for PII detection.");
                return false;
            }

            logger.LogInformation(String.Format("AZURE_OPENAI_ENDPOINT is: {0}.", languageEndpoint == null ? String.Empty : languageEndpoint));

            TextAnalyticsClient client = new TextAnalyticsClient(languageEndpoint, new DefaultAzureCredential());
            PiiEntityCollection PIIEntities = client.RecognizePiiEntities(CustomContent).Value;

            foreach (PiiEntity piiEntity in PIIEntities)
            {
                logger.LogInformation(String.Format("  PII Detected: <{0}> or type <{1}> with score <{2}%>.", piiEntity.Text, piiEntity.Category, piiEntity.ConfidenceScore * 100));
            }

            bool containsPII = false;
            if (PIIEntities.Count > 0)
            {
                logger.LogWarning("    PII detected.");
                containsPII = true;
            }

            return containsPII;

        }

        private bool RedactPIIIfPresent(ILogger logger, Uri? languageEndpoint = null, AzureKeyCredential? languageKey = null)
        {
            logger.LogInformation("Entering AzureFunctions:RedactPIIIfPresent.");

            if (languageEndpoint == null || languageKey == null)
            {
                logger.LogError("Language endpoint and key are necessary for PII detection and redaction.");
                return false;
            }

            logger.LogInformation(String.Format("AZURE_OPENAI_ENDPOINT is: {0}.", languageEndpoint == null ? String.Empty : languageEndpoint));

            TextAnalyticsClient client = new TextAnalyticsClient(languageEndpoint, new DefaultAzureCredential());
            PiiEntityCollection PIIEntities = client.RecognizePiiEntities(CustomContent).Value;

            foreach (PiiEntity piiEntity in PIIEntities)
            {
                logger.LogInformation(String.Format("  PII Detected: <{0}> or type <{1}> with score <{2}%>.", piiEntity.Text, piiEntity.Category, piiEntity.ConfidenceScore * 100));
            }

            logger.LogInformation(String.Format("Custom Content post PII Redaction is: {0}.", PIIEntities.RedactedText == null ? String.Empty : PIIEntities.RedactedText));
            CustomContent = PIIEntities.RedactedText;
            return true;

        }

        private bool HarmfulContentPresent(ILogger logger, Uri? languageEndpoint = null, AzureKeyCredential? languageKey = null)
        {
            logger.LogInformation("Entering AzureFunctions:HarmfulContentPresent.");

            if (languageEndpoint == null || languageKey == null)
            {
                logger.LogError("Language endpoint and key are necessary for Harmful Content detection.");
                return false;
            }

            logger.LogInformation(String.Format("AZURE_OPENAI_ENDPOINT is: {0}.", languageEndpoint == null ? String.Empty : languageEndpoint));

            ContentSafetyClient client = new ContentSafetyClient(languageEndpoint, languageKey);
            AnalyzeTextResult ContentSafetyEntitities = client.AnalyzeText(CustomContent).Value;

            bool isUnsafe = false;
            foreach (TextCategoriesAnalysis entity in ContentSafetyEntitities.CategoriesAnalysis)
            {
                logger.LogInformation(String.Format("  Harmful content analysis for  <{0}> has returned <{1}>.", entity.Category, entity.Severity));
                if (entity.Severity > 2) // Severity: 0=safe, 2=low, 4=medium, 6=high
                {
                    logger.LogWarning("    Harmful content detected.");
                    isUnsafe = true;
                }
            }

            return isUnsafe;

        }

        private bool GenerateBodyContent(ILogger logger, Uri? languageEndpoint = null, AzureKeyCredential? languageKey = null, string? modelName = "")
        {
            logger.LogInformation("Entering AzureFunctions:GenerateBodyContent.");

            if (languageEndpoint == null || languageKey == null || String.IsNullOrEmpty(modelName))
            {
                logger.LogError("Language endpoint, key, and model name are necessary for AI Body Content generation.");
                return false;
            }

            logger.LogInformation(String.Format("AZURE_OPENAI_ENDPOINT is: {0}.", languageEndpoint == null ? String.Empty : languageEndpoint));
            logger.LogInformation(String.Format("AZURE_OPENAI_MODEL is: {0}.", modelName == null ? String.Empty : modelName));

            AzureOpenAIClient azureClient = new AzureOpenAIClient(languageEndpoint, new DefaultAzureCredential(), new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2024_12_01_Preview));
            ChatClient chatClient = azureClient.GetChatClient(modelName);

            logger.LogInformation(String.Format("Requesting body to OpenAI: {0}.", CustomContent));
            ChatCompletion completion = chatClient.CompleteChat(
                [
                    new SystemChatMessage("You are a helpful assistant that helps writing business email in a work environment. Emails shall be polite and professional."),
                    new UserChatMessage(CustomContent)
                ]);
            logger.LogInformation(String.Format("Response from OpenAI: {0}.", completion.Content[0].Text));

            CustomContent = completion.Content[0].Text;
            logger.LogInformation("Content generated successfully and CustomContent updated with Azure OpenAI text.");

            return true;
        }
    }
}