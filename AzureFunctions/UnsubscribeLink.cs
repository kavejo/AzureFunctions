// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Extensions.Logging;
using System.Text;

namespace AzureFunctions
{
    internal class UnsubscribeLink
    {
        public string Subscription { get; set; } = String.Empty;
        public string ResourceGroup { get; set; } = String.Empty;
        public string EmailService { get; set; } = String.Empty;
        public string Domain { get; set; } = String.Empty;
        public string SuppressionList { get; set; } = String.Empty;
        public string EmailRecipient { get; set; } = String.Empty;
        public string OperationId { get; set; } = String.Empty;

        public UnsubscribeLink()
        {
            Subscription = String.Empty;
            ResourceGroup = String.Empty;
            EmailService = String.Empty;
            Domain = String.Empty;
            SuppressionList = String.Empty;
            EmailRecipient = String.Empty;
            OperationId = String.Empty;
        }

        public UnsubscribeLink(string emailRecipient)
        {
            Subscription = Environment.GetEnvironmentVariable("UNSUB_SUBSCRIPTION");
            ResourceGroup = Environment.GetEnvironmentVariable("UNSUB_RESOURCE_GROUP");
            EmailService = Environment.GetEnvironmentVariable("UNSUB_EMAIL_SERVICE");
            Domain = Environment.GetEnvironmentVariable("UNSUB_DOMAIN");
            SuppressionList = Environment.GetEnvironmentVariable("UNSUB_SUPPRESSION_LIST");
            EmailRecipient = emailRecipient;
            OperationId = Guid.NewGuid().ToString();
        }

        public string GnerateUnsubscribeKey(ILogger logger, string functionScheme, string fucntionUrl)
        {
            logger.LogInformation("Entering AzureFunctions:GnerateUnsubscribeLink.");
            string serviceEndpoint = String.Format("{0}://{1}/api/Unsubscribe?UnsubscribeKey=", functionScheme, fucntionUrl);
            string queryString = String.Format("Subscription={0}&ResourceGroup={1}&EmailService={2}&Domain={3}&SuppressionList={4}&EmailRecipient={5}&OperationId={6}", Subscription, ResourceGroup, EmailService, Domain, SuppressionList, EmailRecipient, OperationId);
            logger.LogInformation(String.Format("  Unsubscribe link query string: {0}.", queryString));

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(queryString);
            string encodedQueryString = Convert.ToBase64String(plainTextBytes);
            logger.LogInformation(String.Format("  Unsubscribe key: {0}.", encodedQueryString));

            string unsubscribeLink = String.Concat(serviceEndpoint, encodedQueryString);
            logger.LogInformation(String.Format("  Unsubscribe link: {0}.", unsubscribeLink));
            return unsubscribeLink;
        }

        public UnsubscribeLink GenerateFromUnsubscribeKey(ILogger logger, string unsubscribeKey)
        {
            logger.LogInformation("Entering AzureFunctions:GenerateFromUnsubscribeKey.");
            logger.LogInformation(String.Format("  Unsubscribe key: {0}.", unsubscribeKey));

            byte[] plainTextBytes = Convert.FromBase64String(unsubscribeKey);
            string decodedQueryString = Encoding.UTF8.GetString(plainTextBytes);
            logger.LogInformation(String.Format("  Decoded unsubscribe query string: {0}.", decodedQueryString));

            Dictionary<string, string> queryParams = decodedQueryString.Split('&')
                .Select(param => param.Split('='))
                .ToDictionary(split => split[0], split => split[1]);

            if (!queryParams.ContainsKey("Subscription") || 
                !queryParams.ContainsKey("ResourceGroup") ||
                !queryParams.ContainsKey("EmailService") || 
                !queryParams.ContainsKey("Domain") ||
                !queryParams.ContainsKey("SuppressionList") || 
                !queryParams.ContainsKey("EmailRecipient") ||
                !queryParams.ContainsKey("OperationId"))
            {
                logger.LogError("  Unsubscribe key is missing required parameters.");
                return null;
            }

            Subscription = queryParams["Subscription"];
            logger.LogInformation(String.Format("    Subscription: {0}.", Subscription));
            ResourceGroup = queryParams["ResourceGroup"];
            logger.LogInformation(String.Format("    ResourceGroup: {0}.", ResourceGroup));
            EmailService = queryParams["EmailService"];
            logger.LogInformation(String.Format("    EmailService: {0}.", EmailService));
            Domain = queryParams["Domain"];
            logger.LogInformation(String.Format("    Domain: {0}.", Domain));
            SuppressionList = queryParams["SuppressionList"];
            logger.LogInformation(String.Format("    SuppressionList: {0}.", SuppressionList));
            EmailRecipient = queryParams["EmailRecipient"];
            logger.LogInformation(String.Format("    EmailRecipient: {0}.", EmailRecipient));
            OperationId = queryParams["OperationId"];
            logger.LogInformation(String.Format("    OperationId: {0}.", OperationId));

            return this;
        }
    }
}
