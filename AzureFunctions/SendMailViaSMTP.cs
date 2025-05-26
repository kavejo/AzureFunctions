using Azure;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;
using System.Text.Json;

namespace AzureFunctions;

public class SendMailViaSMTP
{
    private readonly ILogger<SendMailViaSMTP> _logger;
    private EmailMessageRequest? _emailMessageRequest = null!;
    private readonly List<string> _mandatoryConfigurationEntries = new List<string> { "ALLOWED_HOSTS", "ACS_SMTP_ENDPOINT", "ACS_SMTP_PORT", "ACS_SMTP_USERNAME", "ACS_SMTP_PASSWORD", "AZURE_OPENAI_ENDPOINT", "AZURE_OPENAI_KEY", "AZURE_OPENAI_MODEL" };
    private readonly List<string> _mandatoryNumericConfigurationEntries = new List<string> { "ACS_SMTP_PORT" };
    private readonly string? _smtpEndpoint = Environment.GetEnvironmentVariable("ACS_SMTP_ENDPOINT");
    private readonly string? _smtpPort = Environment.GetEnvironmentVariable("ACS_SMTP_PORT");
    private readonly string? _smtpUsername = Environment.GetEnvironmentVariable("ACS_SMTP_USERNAME");
    private readonly string? _smtpPassword = Environment.GetEnvironmentVariable("ACS_SMTP_PASSWORD");
    private static string? _openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    private static string? _openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
    private static string? _modelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL");
    private int _numericSmtpPort = 0;

    public SendMailViaSMTP(ILogger<SendMailViaSMTP> logger)
    {
        _logger = logger;
    }

    [Function("SendMailViaSMTP")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "GET", "POST")] HttpRequest req)
    {
        _logger.LogInformation("Entering AzureFunctions:SendMailViaSMTP.");
        _logger.LogInformation(String.Format("Host: {0}.", req.Host));
        _logger.LogInformation(String.Format("Method: {0}.", req.Method));

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
            _logger.LogInformation("Message will be built with all default values");
            _emailMessageRequest = new EmailMessageRequest();
            if (_emailMessageRequest == null)
            {
                return new UnprocessableEntityObjectResult("Unable to initialize message with default values.");
            }
        }

        bool isProcessed = _emailMessageRequest.ProcessContent(_logger, new Uri(_openAIEndpoint), new AzureKeyCredential(_openAIKey), _modelName);
        if (!isProcessed)
        {
            return new BadRequestObjectResult("Failed to process message.");
        }

        try
        {
            _logger.LogInformation("Preparing to send email message via Azure Communication Services using SMTP Submission Client.");
            MemoryStream ProtocolLogStream = new MemoryStream();
            SmtpClient client = new SmtpClient(new ProtocolLogger(ProtocolLogStream));
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.SslProtocols = SslProtocols.Tls12;
            client.RequireTLS = true;
            client.Connect(_smtpEndpoint, _numericSmtpPort, SecureSocketOptions.StartTls);
            client.Authenticate(_smtpUsername, _smtpPassword);
            string result = client.Send(_emailMessageRequest.RetrieveMessageForSMTP(_logger));
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
            _logger.LogError(String.Format("An error occurred while sending the email message via Azure Communication Services Email using SMTP Submission Client. Exception: {0}", ex.Message));
            return new ObjectResult(String.Format("An error occurred while sending the email message via Azure Communication Services Email using SMTP Submission Client.Exception: {0}", ex.Message))
            {
                StatusCode = 500,
            };
        }
    }
}