using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions;

public class Unsubscribe
{
    private readonly ILogger<Unsubscribe> _logger;
    private readonly List<string> _mandatoryConfigurationEntries = new List<string> { "ALLOWED_HOSTS", "UNSUB_SUBSCRIPTION", "UNSUB_RESOURCE_GROUP", "UNSUB_EMAIL_SERVICE", "UNSUB_DOMAIN", "UNSUB_SUPPRESSION_LIST" };
    private UnsubscribeLink? _unsubscribeLink = null;


    public Unsubscribe(ILogger<Unsubscribe> logger)
    {
        _logger = logger;
    }

    [Function("Unsubscribe")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "GET")] HttpRequest req)
    {
        _logger.LogInformation("Entering AzureFunctions:Unsubscribe.");
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
        // Avoiding to chcck the IP as while the mail sending has to be restricted to known clients, the unsubscribe link should be available to anyone who has it.
        //if (!AnalyzeRequestIP.IsIpAllowed(req.HttpContext.Connection.RemoteIpAddress, _logger))
        //{
        //    return new UnauthorizedObjectResult(String.Format("Requests coming from IP {0} are not allowed.", req.HttpContext.Connection.RemoteIpAddress));
        //}
        if (!String.Equals(req.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return new BadRequestObjectResult(String.Format("Requests method {0} is not allowed.", req.Method));
        }

        if (req.Query.Count <= 0)
        {
            return new BadRequestObjectResult("UnsubscribeKey is missing as no query parameters have been specified.");
        }

        foreach (var queryParameter in req.Query)
        {
            _logger.LogInformation(String.Format("  Query parameter: {0} = {1}", queryParameter.Key, queryParameter.Value));
        }

        if (!req.Query.ContainsKey("UnsubscribeKey") || String.IsNullOrEmpty(req.Query["UnsubscribeKey"]))
        {
            return new BadRequestObjectResult("UnsubscribeKey is either missing or empty");
        }

        _logger.LogInformation("Decoding parameters.");
        string unsubscribeKey = req.Query["UnsubscribeKey"];
        _unsubscribeLink = new UnsubscribeLink();
        _unsubscribeLink.GenerateFromUnsubscribeKey(_logger, unsubscribeKey);

        if (_unsubscribeLink == null)
        {
            return new BadRequestObjectResult("Failed to decode Unsubscribe parameters from the UnsubscribeKey key.");
        }

        try
        {
            _logger.LogInformation("  Connecting to Azure ARM.");
            ArmClient client = new ArmClient(new DefaultAzureCredential());
            _logger.LogInformation("  Binding to the Suppression List.");
            ResourceIdentifier suppressionListAddressResourceId = SuppressionListAddressResource.CreateResourceIdentifier(_unsubscribeLink.Subscription, _unsubscribeLink.ResourceGroup, _unsubscribeLink.EmailService, _unsubscribeLink.Domain, _unsubscribeLink.SuppressionList, _unsubscribeLink.OperationId);
            _logger.LogInformation("  Fetching Suppression Lists.");
            SuppressionListAddressResource suppressionListAddressResource = client.GetSuppressionListAddressResource(suppressionListAddressResourceId);

            SuppressionListAddressResourceData suppressionListAddressData = new SuppressionListAddressResourceData()
            {
                Email = _unsubscribeLink.EmailRecipient,
                Notes = String.Format("Added via AzureFunctions:Unsubscribe on {0:D2}-{1:D2}-{2:D2} at {3:D2}:{4:D2}:{5:D2}",
                        DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day,
                        DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, DateTime.Now.TimeOfDay.Seconds
                        )
            };

            _logger.LogInformation("  Updating Suppression List.");
            suppressionListAddressResource.Update(WaitUntil.Completed, suppressionListAddressData);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to update the suppression list.");
            return new ObjectResult("Failed to update the suppression list.")
            {
                StatusCode = 500,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while processing the unsubscribe request.");
            return new ObjectResult("An unexpected error occurred while processing the unsubscribe request.")
            {
                StatusCode = 500,
            };
        }

        return new OkObjectResult(String.Format("The email address {0} has been added to the Suppression List.", _unsubscribeLink.EmailRecipient));
    }
}