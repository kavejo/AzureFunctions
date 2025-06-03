// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Azure;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;
using System.Text.Json;

namespace AzureFunctions;

public class SendMailViaEXCH
{
    private readonly ILogger<SendMailViaEXCH> _logger;
    private EmailMessageRequest? _emailMessageRequest = null!;
    private readonly List<string> _mandatoryConfigurationEntries = new List<string> { "ALLOWED_HOSTS", "EXCHANGE_SMTP_ENDPOINT", "EXCHANGE_SMTP_PORT", "EXCHANGE_SMTP_USERNAME", "EXCHANGE_SMTP_PASSWORD", "AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_KEY", "AZURE_OPENAI_MODEL", "DEFAULT_SENDER", "DEFAULT_RECIPIENT", "UNSUB_SUBSCRIPTION", "UNSUB_RESOURCE_GROUP", "UNSUB_EMAIL_SERVICE", "UNSUB_DOMAIN", "UNSUB_SUPPRESSION_LIST" };
    private readonly List<string> _mandatoryNumericConfigurationEntries = new List<string> { "EXCHANGE_SMTP_PORT" };
    private readonly string? _smtpEndpoint = Environment.GetEnvironmentVariable("EXCHANGE_SMTP_ENDPOINT");
    private readonly string? _smtpPort = Environment.GetEnvironmentVariable("EXCHANGE_SMTP_PORT");
    private readonly string? _smtpUsername = Environment.GetEnvironmentVariable("EXCHANGE_SMTP_USERNAME");
    private readonly string? _smtpPassword = Environment.GetEnvironmentVariable("EXCHANGE_SMTP_PASSWORD");
    private static string? _openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    private static string? _openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
    private static string? _modelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL");
    private int _numericSmtpPort = 0;

    public SendMailViaEXCH(ILogger<SendMailViaEXCH> logger)
    {
        _logger = logger;
    }

    [Function("SendMailViaEXCH")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "GET", "POST")] HttpRequest req)
    {
        _logger.LogInformation("Entering AzureFunctions:SendMailViaEXCH.");
        _logger.LogInformation(String.Format("Scheme: {0}.", req.Scheme));
        _logger.LogInformation(String.Format("Host: {0}.", req.Host));
        _logger.LogInformation(String.Format("Method: {0}..", req.Method));

        if (!AssertConfiguration.VerifyConfiguratiopnEntriesExistence(_logger, _mandatoryConfigurationEntries))
        {
            return new ObjectResult(String.Format("One or more of the following environment variables are missing: {0}.", String.Join(",", _mandatoryConfigurationEntries)))
            {
                StatusCode = 500,
            };
        }
        if (!AssertConfiguration.VerifyNumericConfigurationEntries(_logger, _mandatoryNumericConfigurationEntries))
        {
            return new ObjectResult(String.Format("One or more of the following environment variables is not an Integer: {0}.", String.Join(",", _mandatoryConfigurationEntries)))
            {
                StatusCode = 500,
            };
        }

        if (!AnalyzeRequestIP.IsIpAllowed(req.HttpContext.Connection.RemoteIpAddress, _logger))
        {
            return new UnauthorizedObjectResult(String.Format("Requests coming from IP {0} are not allowed.", req.HttpContext.Connection.RemoteIpAddress));
        }
        if (!String.Equals(req.Method, "GET", StringComparison.OrdinalIgnoreCase) && !String.Equals(req.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return new BadRequestObjectResult(String.Format("Requests method {0} is not allowed.", req.Method));
        }

        bool conversionSuccessed = int.TryParse(_smtpPort, out _numericSmtpPort);
        if (!conversionSuccessed)
        {
            return new UnprocessableEntityObjectResult(String.Format("Unable to convert SMTP port number: {0}.", _smtpPort));
        }
        if (_numericSmtpPort <= 0)
        {
            return new UnprocessableEntityObjectResult(String.Format("Invalid SMTP port number: {0}.", _smtpPort));
        }


        if (String.Equals(req.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            if (req.ContentType != "application/json")
            {
                return new BadRequestObjectResult("Invalid content type. Expected application/json.");
            }
            if (req.Body == null)
            {
                return new BadRequestObjectResult("Empty request body. Post request is expected to supply content.");
            }
            else
            {
                using (StreamReader sr = new StreamReader(req.Body))
                {
                    _logger.LogInformation("Message will be built with post body content. Missing values will be set to default.");
                    string bodyContent = await sr.ReadToEndAsync();
                    if (String.IsNullOrEmpty(bodyContent))
                    {
                        return new UnprocessableEntityObjectResult("Unable to read request body.");
                    }
                    _emailMessageRequest = JsonSerializer.Deserialize<EmailMessageRequest>(bodyContent);
                    if (_emailMessageRequest == null)
                    {
                        return new UnprocessableEntityObjectResult("Unable to deserialize request body.");
                    }
                }
            }
        }

        if (String.Equals(req.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            _emailMessageRequest = new EmailMessageRequest();
            if (_emailMessageRequest == null)
            {
                return new UnprocessableEntityObjectResult("Unable to initialize message with default values.");
            }
            if (req.Query.Count > 0)
            {
                _logger.LogInformation("Message will be built with query parameters. Missing values will be set to default.");
                foreach (var queryParameter in req.Query)
                {
                    _logger.LogInformation(String.Format("  Query parameter: {0} = {1}", queryParameter.Key, queryParameter.Value));
                }
                if (req.Query.ContainsKey("Type") && !String.IsNullOrEmpty(req.Query["Type"]))
                {
                    _emailMessageRequest.Type = (EmailMessageRequestType)Convert.ToInt32(req.Query["Type"]);
                    _logger.LogInformation(String.Format("Type overridden to: {0}.", req.Query["Type"]));
                }
                if (req.Query.ContainsKey("From") && !String.IsNullOrEmpty(req.Query["From"]))
                {
                    _emailMessageRequest.From = req.Query["From"];
                    _logger.LogInformation(String.Format("From overridden to: {0}.", req.Query["From"]));
                }
                if (req.Query.ContainsKey("ReplyTo") && !String.IsNullOrEmpty(req.Query["ReplyTo"]))
                {
                    _emailMessageRequest.ReplyTo = req.Query["ReplyTo"];
                    _logger.LogInformation(String.Format("ReplyTo overridden to: {0}.", req.Query["ReplyTo"]));
                }
                if (req.Query.ContainsKey("To") && !String.IsNullOrEmpty(req.Query["To"]))
                {
                    _emailMessageRequest.To = req.Query["To"];
                    _logger.LogInformation(String.Format("To overridden to: {0}.", req.Query["To"]));
                }
                if (req.Query.ContainsKey("Subject") && !String.IsNullOrEmpty(req.Query["Subject"]))
                {
                    _emailMessageRequest.Subject = req.Query["Subject"];
                    _logger.LogInformation(String.Format("Subject overridden to: {0}.", req.Query["Subject"]));
                }
                if (req.Query.ContainsKey("TextBody") && !String.IsNullOrEmpty(req.Query["TextBody"]))
                {
                    _emailMessageRequest.TextBody = req.Query["TextBody"];
                    _logger.LogInformation(String.Format("TextBody overridden to: {0}.", req.Query["TextBody"]));
                }
                if (req.Query.ContainsKey("HtmlBody") && !String.IsNullOrEmpty(req.Query["HtmlBody"]))
                {
                    _emailMessageRequest.HtmlBody = req.Query["HtmlBody"];
                    _logger.LogInformation(String.Format("HtmlBody overridden to: {0}.", req.Query["HtmlBody"]));
                }
                if (req.Query.ContainsKey("CustomContent") && !String.IsNullOrEmpty(req.Query["CustomContent"]))
                {
                    _emailMessageRequest.CustomContent = req.Query["CustomContent"];
                    _logger.LogInformation(String.Format("CustomContent overridden to: {0}.", req.Query["CustomContent"]));
                }
            }
            else
            {
                _logger.LogInformation("Message built with all default values.");
            }
        }

        try
        {
            bool isProcessed = _emailMessageRequest.ProcessContent(_logger, new Uri(_openAIEndpoint), new AzureKeyCredential(_openAIKey), _modelName);
            if (!isProcessed)
            {
                return new BadRequestObjectResult("Failed to process message.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(String.Format("An error occurred while preparing the message content. Exception: {0}", ex.Message));
            return new ObjectResult(String.Format("An error occurred while preparing the message content. Exception: {0}", ex.Message))
            {
                StatusCode = 500,
            };
        }

        MemoryStream ProtocolLogStream = new MemoryStream();

        try
        {
            _logger.LogInformation("Generating unique unsubscribe link.");
            UnsubscribeLink unsubscribeLinkObject = new UnsubscribeLink(_emailMessageRequest.To);
            string unsubscribeLink = unsubscribeLinkObject.GnerateUnsubscribeKey(_logger, req.Scheme, req.Host.ToString());

            _logger.LogInformation("Generating e-maail message to send.");
            MimeMessage emailMessage = _emailMessageRequest.RetrieveMessageForSMTP(_logger, unsubscribeLink);

            _logger.LogInformation("Preparing to send email message via Exchange Server using SMTP Submission Client with Basic Authentication.");
            SmtpClient client = new SmtpClient(new ProtocolLogger(ProtocolLogStream));
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.SslProtocols = SslProtocols.Tls12;
            client.RequireTLS = true;
            client.Connect(_smtpEndpoint, _numericSmtpPort, SecureSocketOptions.StartTls);
            client.Authenticate(_smtpUsername, _smtpPassword);
            string result = client.Send(emailMessage);
            client.Disconnect(true);

            StreamReader ProtocolLogReader = new StreamReader(ProtocolLogStream);
            ProtocolLogStream.Seek(0, SeekOrigin.Begin);
            string ProtocolLogContent = ProtocolLogReader.ReadToEnd();
            ProtocolLogStream.Close();

            _logger.LogInformation(ProtocolLogContent);
            _logger.LogInformation(String.Format("Email message processed successfully. The status is: {0}.", result));
            return new OkObjectResult(String.Format("Email message processed successfully. The status is: {0}.", result));
        }
        catch (Exception ex)
        {
            if (ProtocolLogStream.CanRead)
            {
                StreamReader ProtocolLogReader = new StreamReader(ProtocolLogStream);
                ProtocolLogStream.Seek(0, SeekOrigin.Begin);
                string ProtocolLogContent = ProtocolLogReader.ReadToEnd();
                ProtocolLogStream.Close();
                _logger.LogInformation(ProtocolLogContent);
            }
            else
            {
                _logger.LogWarning("Protocol log stream is not readable.");
            }
            _logger.LogError(String.Format("An error occurred while sending the email message via Exchange Server using SMTP Submission Client with Basic Authentication. Exception: {0}", ex.Message));
            return new ObjectResult(String.Format("An error occurred while sending the email message via Exchange Server using SMTP Submission Client with Basic Authentication. Exception: {0}", ex.Message))
            {
                StatusCode = 500,
            };
        }
    }
}