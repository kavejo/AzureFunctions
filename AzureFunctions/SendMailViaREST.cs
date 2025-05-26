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
    private readonly List<string> _mandatoryConfigurationEntries = new List<string> { "ALLOWED_HOSTS", "ACS_EMAIL_ENDPOINT", "AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_KEY", "AZURE_OPENAI_MODEL" };
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
            _logger.LogInformation("Preparing to send email message via Azure Communication Services Email using REST API.");
            EmailClient emailClient = new EmailClient(new Uri(_resourceEndpoint), new DefaultAzureCredential());
            EmailSendOperation emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, _emailMessageRequest.RetrieveMessageForREST(_logger));

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