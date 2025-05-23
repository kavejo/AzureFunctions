using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AzureFunctions
{
    public class EmailMessageRequest
    {
        public EmailMessageRequestType Type { get; set; } = EmailMessageRequestType.SendMail;
        public string From { get; set; } = "NoReply@Toniolo.cloud";
        public string ReplyTo { get; set; } = "NoReply@Toniolo.cloud";
        public string To { get; set; } = "totoni@microsoft.com";
        public string Subject { get; set; } = String.Format("AzureFunctions:EmailMessage Test performed on {0:D2}-{1:D2}-{2:D2} at {3:D2}:{4:D2}:{5:D2} by Tommaso Toniolo", 
            DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day, 
            DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, DateTime.Now.TimeOfDay.Seconds
        );
        public string TextBody { get; set; } = @"Hello! \r\n This is a test e-mail message sent to you by Tommaso Toniolo via Azure Communication Services. \r\n _PLACEHOLDER_ \r\n This message has been sent to you as a test. \r\n Please feel free to ignore it. \r\n TONIOLO.DEV & TONIOLO.CLOUD \r\n For more information: https://aka.ms/totoni\r\n";
        public string HtmlBody { get; set; } = @"<!doctype html><html lang=""en""><head><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8""><title>Test email sent via Azure Communication Services Email</title><style media=""all"" type=""text/css"">@media all {.btn-primary table td:hover {background-color: #ec0867 !important;}.btn-primary a:hover {background-color: #ec0867 !important;border-color: #ec0867 !important;}}@media only screen and (max-width: 640px) {.main p,.main td,.main span {font-size: 16px !important;}.wrapper {padding: 8px !important;}.content {padding: 0 !important;}.container {padding: 0 !important;padding-top: 8px !important;width: 100% !important;}.main {border-left-width: 0 !important;border-radius: 0 !important;border-right-width: 0 !important;}.btn table {max-width: 100% !important;width: 100% !important;}.btn a {font-size: 16px !important;max-width: 100% !important;width: 100% !important;}}@media all {.ExternalClass {width: 100%;}.ExternalClass,.ExternalClass p,.ExternalClass span,.ExternalClass font,.ExternalClass td,.ExternalClass div {line-height: 100%;}.apple-link a {color: inherit !important;font-family: inherit !important;font-size: inherit !important;font-weight: inherit !important;line-height: inherit !important;text-decoration: none !important;}#MessageViewBody a {color: inherit;text-decoration: none;font-size: inherit;font-family: inherit;font-weight: inherit;line-height: inherit;}}</style></head><body style=""font-family: Helvetica, sans-serif; -webkit-font-smoothing: antialiased; font-size: 16px; line-height: 1.3; -ms-text-size-adjust: 100%; -webkit-text-size-adjust: 100%; background-color: #f4f5f6; margin: 0; padding: 0;""><table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""body"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #f4f5f6; width: 100%;"" width=""100%"" bgcolor=""#f4f5f6""><tr><td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top;"" valign=""top"">&nbsp;</td><td class=""container"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; max-width: 600px; padding: 0; padding-top: 24px; width: 600px; margin: 0 auto;"" width=""600"" valign=""top""><div class=""content"" style=""box-sizing: border-box; display: block; margin: 0 auto; max-width: 600px; padding: 0;""><!-- START CENTERED WHITE CONTAINER --><span class=""preheader"" style=""color: transparent; display: none; height: 0; max-height: 0; max-width: 0; opacity: 0; overflow: hidden; mso-hide: all; visibility: hidden; width: 0;"">This is the demo e-mail message preview. Feel free to ignore it.</span><table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""main"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; background: #ffffff; border: 1px solid #eaebed; border-radius: 16px; width: 100%;"" width=""100%""> <!-- START MAIN CONTENT AREA --> <tr> <td class=""wrapper"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; box-sizing: border-box; padding: 24px;"" valign=""top""> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">Hello!</p> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">This is a test e-mail message sent to you by <b>Tommaso Toniolo</b> via Azure Communication Services.</p> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">_PLACEHOLDER_</p> <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" class=""btn btn-primary"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; box-sizing: border-box; width: 100%; min-width: 100%;"" width=""100%""> <tbody> <tr> <td align=""left"" style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; padding-bottom: 16px;"" valign=""top""> <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: auto;""> <tbody> <tr> <td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top; border-radius: 4px; text-align: center; background-color: #0867ec;"" valign=""top"" align=""center"" bgcolor=""#0867ec""> <a href=""https://aka.ms/totoni"" target=""_blank"" style=""border: solid 2px #0867ec; border-radius: 4px; box-sizing: border-box; cursor: pointer; display: inline-block; font-size: 16px; font-weight: bold; margin: 0; padding: 12px 24px; text-decoration: none; text-transform: capitalize; background-color: #0867ec; border-color: #0867ec; color: #ffffff;"">View Sender Details!</a> </td> </tr> </tbody> </table> </td> </tr> </tbody> </table> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">This message has been sent to you as a test.</p> <p style=""font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;"">Please feel free to ignore it.</p> </td> </tr> <!-- END MAIN CONTENT AREA --> </table><!-- START FOOTER --><div class=""footer"" style=""clear: both; padding-top: 24px; text-align: center; width: 100%;""> <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%;"" width=""100%""> <tr> <td class=""content-block"" style=""font-family: Helvetica, sans-serif; vertical-align: top; color: #9a9ea6; font-size: 16px; text-align: center;"" valign=""top"" align=""center""> <span class=""apple-link"" style=""color: #9a9ea6; font-size: 16px; text-align: center;"">TONIOLO.DEV & TONIOLO.CLOUD</span> </td> </tr> </table></div><!-- END FOOTER --><!-- END CENTERED WHITE CONTAINER --></div></td><td style=""font-family: Helvetica, sans-serif; font-size: 16px; vertical-align: top;"" valign=""top"">&nbsp;</td></tr></table></body></html>";
        public string CustomContent { get; set; } = String.Empty;

        public bool ProcessContent(ILogger logger) 
        {
            // To be implemented: this method should be used to process the content of the message before sending, it based on the value of EmailMessageRequestType Type
            return true; 
        }

        public EmailMessage RetrieveMessageForREST(ILogger logger)
        {
            TextBody = TextBody.Replace("_PLACEHOLDER_", CustomContent);
            HtmlBody = HtmlBody.Replace("_PLACEHOLDER_", CustomContent);

            EmailContent emailContent = new EmailContent(Subject);
            emailContent.PlainText = TextBody;
            emailContent.Html = HtmlBody;

            EmailMessage emailMessage = new EmailMessage(From, To, emailContent);
            emailMessage.ReplyTo.Add(new EmailAddress(ReplyTo));

            return emailMessage;
        }

        public MimeMessage RetrieveMessageForSMTP(ILogger logger)
        {
            TextBody = TextBody.Replace("_PLACEHOLDER_", CustomContent);
            HtmlBody = HtmlBody.Replace("_PLACEHOLDER_", CustomContent);

            MimeMessage emailMessage = new MimeMessage();
            emailMessage.From.Add(MailboxAddress.Parse(From));
            emailMessage.Headers.Add("Reply-To", ReplyTo);
            emailMessage.To.Add(MailboxAddress.Parse(To));
            emailMessage.Subject = Subject;

            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.TextBody = TextBody;
            bodyBuilder.HtmlBody = HtmlBody;
            emailMessage.Body = bodyBuilder.ToMessageBody();
            
            return emailMessage;
        }
    }
}