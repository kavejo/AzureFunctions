using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzureFunctions;

public class SendMailViaREST
{
    private readonly ILogger<SendMailViaREST> _logger;
    private EmailMessageRequest? _emailMessageRequest = null!;
    private readonly List<string> _mandatoryConfigurationEntries = new List<string> { "ALLOWED_HOSTS", "ACS_EMAIL_ENDPOINT", "AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_KEY", "AZURE_OPENAI_MODEL", "DEFAULT_SENDER", "DEFAULT_RECIPIENT", "UNSUB_SUBSCRIPTION", "UNSUB_RESOURCE_GROUP", "UNSUB_EMAIL_SERVICE", "UNSUB_DOMAIN", "UNSUB_SUPPRESSION_LIST" };
    private readonly string? _resourceEndpoint = Environment.GetEnvironmentVariable("ACS_EMAIL_ENDPOINT");
    private static string? _openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    private static string? _openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
    private static string? _modelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL");

    public SendMailViaREST(ILogger<SendMailViaREST> logger)
    {
        _logger = logger;
    }

    [Function("SendMailViaREST")]
    public async Task<IActionResult> Run( [HttpTrigger(AuthorizationLevel.Anonymous, "GET", "POST")] HttpRequest req)
    {
        _logger.LogInformation("Entering AzureFunctions:SendMailViaREST.");
        _logger.LogInformation(String.Format("Scheme: {0}.", req.Scheme));
        _logger.LogInformation(String.Format("Host: {0}.", req.Host));
        _logger.LogInformation(String.Format("Method: {0}.", req.Method));

        if (!AssertConfiguration.VerifyConfiguratiopnEntriesExistence(_logger, _mandatoryConfigurationEntries))
        {
            return new ObjectResult(String.Format("One or more of the following environment variables are missing: {0}.", String.Join(",", _mandatoryConfigurationEntries)))
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

        if ( String.Equals(req.Method, "POST",StringComparison.OrdinalIgnoreCase))
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
            _logger.LogInformation("Message will be built with all default values.");
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
                    _logger.LogInformation(String.Format("  Type overridden to: {0}.", req.Query["Type"]));
                }
                if (req.Query.ContainsKey("From") && !String.IsNullOrEmpty(req.Query["From"]))
                {
                    _emailMessageRequest.From = req.Query["From"];
                    _logger.LogInformation(String.Format("  From overridden to: {0}.", req.Query["From"]));
                }
                if (req.Query.ContainsKey("ReplyTo") && !String.IsNullOrEmpty(req.Query["ReplyTo"]))
                {
                    _emailMessageRequest.ReplyTo = req.Query["ReplyTo"];
                    _logger.LogInformation(String.Format("  ReplyTo overridden to: {0}.", req.Query["ReplyTo"]));
                }
                if (req.Query.ContainsKey("To") && !String.IsNullOrEmpty(req.Query["To"]))
                {
                    _emailMessageRequest.To = req.Query["To"];
                    _logger.LogInformation(String.Format("  To overridden to: {0}.", req.Query["To"]));
                }
                if (req.Query.ContainsKey("Subject") && !String.IsNullOrEmpty(req.Query["Subject"]))
                {
                    _emailMessageRequest.Subject = req.Query["Subject"];
                    _logger.LogInformation(String.Format("  Subject overridden to: {0}.", req.Query["Subject"]));
                }
                if (req.Query.ContainsKey("TextBody") && !String.IsNullOrEmpty(req.Query["TextBody"]))
                {
                    _emailMessageRequest.TextBody = req.Query["TextBody"];
                    _logger.LogInformation(String.Format("  TextBody overridden to: {0}.", req.Query["TextBody"]));
                }
                if (req.Query.ContainsKey("HtmlBody") && !String.IsNullOrEmpty(req.Query["HtmlBody"]))
                {
                    _emailMessageRequest.HtmlBody = req.Query["HtmlBody"];
                    _logger.LogInformation(String.Format("  HtmlBody overridden to: {0}.", req.Query["HtmlBody"]));
                }
                if (req.Query.ContainsKey("CustomContent") && !String.IsNullOrEmpty(req.Query["CustomContent"]))
                {
                    _emailMessageRequest.CustomContent = req.Query["CustomContent"];
                    _logger.LogInformation(String.Format("  CustomContent overridden to: {0}.", req.Query["CustomContent"]));
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

        try
        {
            _logger.LogInformation("Generating unique unsubscribe link.");
            UnsubscribeLink unsubscribeLinkObject = new UnsubscribeLink(_emailMessageRequest.To);
            string unsubscribeLink = unsubscribeLinkObject.GnerateUnsubscribeKey(_logger, req.Scheme, req.Host.ToString());

            _logger.LogInformation("Generating e-maail message to send.");
            EmailMessage emailMessage = _emailMessageRequest.RetrieveMessageForREST(_logger, unsubscribeLink);

            _logger.LogInformation("Preparing to send email message via Azure Communication Services Email using REST API.");
            EmailClient emailClient = new EmailClient(new Uri(_resourceEndpoint), new DefaultAzureCredential());
            EmailSendOperation emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);

            _logger.LogInformation(String.Format("Email message processed successfully. The status is: {0}. The CorrelationID is {1}.", emailSendOperation.Value.Status, emailSendOperation.Id));
            return new OkObjectResult(String.Format("Email message processed successfully. The status is: {0}.The CorrelationID is {1}.", emailSendOperation.Value.Status, emailSendOperation.Id));
        }
        catch (Exception ex) 
        {
            _logger.LogError(String.Format("An error occurred while sending the email message via Azure Communication Services Email using REST API. Exception: {0}", ex.Message));
            return new ObjectResult(String.Format("An error occurred while sending the email message via Azure Communication Services Email using REST API. Exception: {0}", ex.Message))
            {
                StatusCode = 500,
            };
        }


    }
}