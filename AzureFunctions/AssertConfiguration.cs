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
            "EXCHANGE_SMTP_PASSWORD",
            "DEFAULT_SENDER",
            "DEFAULT_RECIPIENT"
        };
        private static List<string> _numericConfiguration = new List<string> {
            "ACS_SMTP_PORT",
            "EXCHANGE_SMTP_PORT"
        };

        public static bool VerifyConfiguratiopnEntriesExistence(ILogger logger, List<string>? variablesToValidate = null)
        {
            logger.LogInformation("Entering AzureFunctions:VerifyConfiguratiopnEntriesExistence."); 
            if (variablesToValidate != null)
            {
                logger.LogInformation("Checking provided variables instead of all config entries.");
                _mandatoryConfiguration = variablesToValidate;
            }

            foreach (string configValue in _mandatoryConfiguration)
            {
                logger.LogInformation(String.Format("  Analysing environment variable: {0}.", configValue));
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(configValue)))
                {
                    logger.LogError(String.Format("    Environment variable {0} is not configured.", configValue));
                    return false;
                }
            }

            logger.LogInformation("Configuration validation completed successfully.");
            return true;
        }

        public static bool VerifyNumericConfigurationEntries(ILogger logger, List<string>? variablesToValidate = null)
        {
            logger.LogInformation("Entering AzureFunctions:VerifyNumericConfigurationEntries.");
            if (variablesToValidate != null)
            {
                logger.LogInformation("Checking provided variables instead of all numeric config entries.");
                _numericConfiguration = variablesToValidate;
            }

            foreach (string configValue in _numericConfiguration)
            {
                string? configValueFromEnv = Environment.GetEnvironmentVariable(configValue);
                logger.LogInformation(String.Format("  Verifying environment variable {0} with value {1}.", configValue, configValueFromEnv));
                if (string.IsNullOrEmpty(configValueFromEnv))
                {
                    logger.LogError(String.Format("    Environment variable {0} is not configured.", configValue));
                    return false;
                }

                int numericValue = 0;
                bool conversionSuccessed = int.TryParse(configValueFromEnv, out numericValue);
                if (!conversionSuccessed)
                {
                    logger.LogError(String.Format("    Unable to convert variable {0} to Integer.", configValue));
                    return false;
                }
                if (numericValue <= 0)
                {
                    logger.LogError(String.Format("    Environment variable {0} is not a positive Integer.", configValue));
                    return false;
                }
            }

            logger.LogInformation("Interger configuration validation completed successfully.");
            return true;
        }
    }
}
