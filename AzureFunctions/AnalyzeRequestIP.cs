using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.CompilerServices;

namespace AzureFunctions
{
    internal class AnalyzeRequestIP
    {
        private readonly static string _allowedHostsString = Environment.GetEnvironmentVariable("ALLOWED_HOSTS");
        private static List<string> _allowedHosts = new List<string>(); 
        private static List<string> _allowedIPs = new List<string> { "127.0.0.1" };

        public static bool IsIpAllowed(IPAddress? ipAddress, ILogger logger)
        {
            logger.LogInformation("Entering AzureFunctions:AnalyzeRequestIP");
            logger.LogInformation(String.Format("Checking if IP {0} is allowed.", ipAddress == null ? String.Empty : ipAddress.ToString()));
            logger.LogInformation(String.Format("ALLOWED_HOSTS are: {0}", _allowedHostsString == null ? String.Empty : _allowedHostsString));

            if (ipAddress == null)
            {
                logger.LogInformation("Request not allowed as either IP Address is NULL");
                return false;
            }
            if (string.IsNullOrEmpty(_allowedHostsString))
            {
                logger.LogInformation("Request not allowed as there are no ALLOWED_HOSTS configured");
                return false;
            }
            _allowedHosts = _allowedHostsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string host in _allowedHosts)
            {
                logger.LogInformation(String.Format("  Resolving hosts: {0}", host));
                IPAddress[] resolvedIPs = Dns.GetHostAddresses(host);
                foreach (var resolvedIP in resolvedIPs)
                {
                    logger.LogInformation(String.Format("    Analysing IP: {0}", resolvedIP.ToString()));
                    if (!_allowedIPs.Contains(resolvedIP.ToString()))
                    {
                        logger.LogInformation(String.Format("    Adding IP {0} to the allowed hosts", resolvedIP.ToString()));
                        _allowedIPs.Add(resolvedIP.ToString());
                    }
                }
            }

            if (!_allowedIPs.Contains(ipAddress.ToString()))
            {
                logger.LogInformation("Request IP is not part of the IP to which ALLOWED_HOSTS resolves");
                return false;
            }

            logger.LogInformation("Request IP is is allowed to make requests");
            return true;
        }

    }
}
