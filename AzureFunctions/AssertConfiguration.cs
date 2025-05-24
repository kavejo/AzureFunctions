using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureFunctions
{
    internal class AssertConfiguration
    {
        private static List<string> _mandatoryConfiguration = new List<string> { 
            "ALLOWED_HOSTS",
            "ACS_EMAIL_ENDPOINT",
            "AZURE_OPENAI_ENDPOINT",
            "AZURE_OPENAI_KEY",
            "AZURE_OPENAI_MODEL",
            "ACS_SMTP_ENDPOINT", 
            "ACS_SMTP_PORT", 
            "ACS_SMTP_USERNAME", 
            "ACS_SMTP_PASSWORD",
            "EXCHANGE_SMTP_ENDPOINT",
            "EXCHANGE_SMTP_PORT",
            "EXCHANGE_SMTP_USERNAME",
            "EXCHANGE_SMTP_PASSWORD"
        };
        
        public static bool ValidateConfigurationEntries(ILogger logger, List<string> variablesToValidate = null)
        {
            logger.LogInformation("Entering AzureFunctions:ValidateConfigurationEntries"); 
            if (variablesToValidate != null)
            {
                logger.LogInformation("Checking all variables as no subset of them is provided");
                _mandatoryConfiguration = variablesToValidate;
            }

            foreach (string configValue in _mandatoryConfiguration)
            {
                logger.LogInformation(String.Format("  Analysing environment variable: {0}", configValue));
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(configValue)))
                {
                    logger.LogInformation(String.Format("    Environment variable {0} is not configured", configValue));
                    return false;
                }
            }

            logger.LogInformation("Configuration validation completed successfully");
            return true;
        }
    }
}
