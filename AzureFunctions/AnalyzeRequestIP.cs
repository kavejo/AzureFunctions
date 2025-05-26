using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

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
                logger.LogError("Request not allowed as either IP Address is NULL");
                return false;
            }
            if (string.IsNullOrEmpty(_allowedHostsString))
            {
                logger.LogError("Request not allowed as there are no ALLOWED_HOSTS configured");
                return false;
            }

            if ((String.Equals(_allowedHostsString, "ALL", StringComparison.OrdinalIgnoreCase) == true) || 
                (String.Equals(_allowedHostsString, "ANY", StringComparison.OrdinalIgnoreCase) == true) ||
                (String.Equals(_allowedHostsString, "*", StringComparison.OrdinalIgnoreCase) == true))
            {
                logger.LogInformation("Request allowed as ALLOWED_HOSTS is set to any of ALL/ANY/* which permits requests from any client");
                return true;
            }

            _allowedHosts = _allowedHostsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string host in _allowedHosts)
            {
                logger.LogInformation(String.Format("  Resolving hosts: {0}", host));
                try
                {
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
                catch (SocketException ex)
                {
                    logger.LogError(ex, String.Format("Failed to resolve host {0} with exception: {1}.", host, ex.Message));
                    continue;
                }
            }

            if (!_allowedIPs.Contains(ipAddress.ToString()))
            {
                logger.LogError("Request IP is not part of the IP to which ALLOWED_HOSTS resolves");
                return false;
            }

            logger.LogInformation("Request IP is allowed to make requests");
            return true;
        }

    }
}
