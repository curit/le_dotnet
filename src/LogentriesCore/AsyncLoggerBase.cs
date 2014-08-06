namespace LogentriesCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Configuration;
    using System.IO;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using Microsoft.WindowsAzure;

    public abstract class AsyncLoggerBase : IAsyncLogger
    {
        // Logentries API server address. 
        protected const string LeApiUrl = "api.logentries.com";

        // New Logentries configuration names.
        protected const string ConfigTokenName = "Logentries.Token";

        // New Logentries configuration names.
        protected const string ConfigAccountKey = "Logentries.AccountKey";

        // New Logentries configuration names.
        protected const string ConfigLocation = "Logentries.Location";

        // Newline char to trim from message for formatting. 
        protected static readonly char[] TrimChars = {'\r', '\n'};

        // Unicode line separator character 
        private const string LineSeparator = "\u2028";

        // Thread that takes care of pushing to the Logentries server
        private Thread WorkerThread { get; set; }

        // Internal queue of messages to be sent.
        private ConcurrentQueue<string> Queue { get; set; }

        protected Stream Stream;

        // Logentries API server certificate. 
        protected static readonly X509Certificate2 LeApiServerCertificate =
            new X509Certificate2(Encoding.UTF8.GetBytes(
                @"-----BEGIN CERTIFICATE-----
MIIFSjCCBDKgAwIBAgIDCQpNMA0GCSqGSIb3DQEBBQUAMGExCzAJBgNVBAYTAlVT
MRYwFAYDVQQKEw1HZW9UcnVzdCBJbmMuMR0wGwYDVQQLExREb21haW4gVmFsaWRh
dGVkIFNTTDEbMBkGA1UEAxMSR2VvVHJ1c3QgRFYgU1NMIENBMB4XDTE0MDQxNTEz
NTcxNVoXDTE2MDkxMzA0MTMzMFowgcExKTAnBgNVBAUTIEhpL1RHbXlmUEpJYTFy
b0NQdlJ1U1NNRVdLOFp0NUtmMRMwEQYDVQQLEwpHVDAzOTM4NjcwMTEwLwYDVQQL
EyhTZWUgd3d3Lmdlb3RydXN0LmNvbS9yZXNvdXJjZXMvY3BzIChjKTEyMS8wLQYD
VQQLEyZEb21haW4gQ29udHJvbCBWYWxpZGF0ZWQgLSBRdWlja1NTTChSKTEbMBkG
A1UEAxMSYXBpLmxvZ2VudHJpZXMuY29tMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A
MIIBCgKCAQEAwGsgjVb/pn7Go1jqNQVFsN+VEMRFpu7bJ5i+Lv/gY9zXBDGULr3d
j9/hB/pa49nLUpy9GsaFru2AjNoveoVoe5ng2QhZRlUn77hxkoZsaiD+rrH/D/Yp
LP3b/pNQg+nNTC81uwbhlxjIoeMSaPGjr1SFjZ1StCprZKFRu3IV+2/wZ+STUz/L
aA3r6J86DRptasbzYMkDyWlUzN3nhYUcPUNrd4jSk+soSDEuDpHMahgRdQBo6Dht
EKCSY+vB5ZIgEydI7mra8ygRjXotvc0zeb8Jvo8ZhyLDwvxjgo9F6Li3h/tfAjRR
4ngV7yg9o8MgXN852GMHpUxzqhygLeyqSQIDAQABo4IBqDCCAaQwHwYDVR0jBBgw
FoAUjPTZkwpHvACgSs5LdW6gtrCyfvwwDgYDVR0PAQH/BAQDAgWgMB0GA1UdJQQW
MBQGCCsGAQUFBwMBBggrBgEFBQcDAjAdBgNVHREEFjAUghJhcGkubG9nZW50cmll
cy5jb20wQQYDVR0fBDowODA2oDSgMoYwaHR0cDovL2d0c3NsZHYtY3JsLmdlb3Ry
dXN0LmNvbS9jcmxzL2d0c3NsZHYuY3JsMB0GA1UdDgQWBBRowYR/aaGeiRRQxbaV
1PI8hS4m9jAMBgNVHRMBAf8EAjAAMHUGCCsGAQUFBwEBBGkwZzAsBggrBgEFBQcw
AYYgaHR0cDovL2d0c3NsZHYtb2NzcC5nZW90cnVzdC5jb20wNwYIKwYBBQUHMAKG
K2h0dHA6Ly9ndHNzbGR2LWFpYS5nZW90cnVzdC5jb20vZ3Rzc2xkdi5jcnQwTAYD
VR0gBEUwQzBBBgpghkgBhvhFAQc2MDMwMQYIKwYBBQUHAgEWJWh0dHA6Ly93d3cu
Z2VvdHJ1c3QuY29tL3Jlc291cmNlcy9jcHMwDQYJKoZIhvcNAQEFBQADggEBAAzx
g9JKztRmpItki8XQoGHEbopDIDMmn4Q7s9k7L9nT5gn5XCXdIHnsSe8+/2N7tW4E
iHEEWC5G6Q16FdXBwKjW2LrBKaP7FCRcqXJSI+cfiuk0uywkGBTXpqBVClQRzypd
9vZONyFFlLGUwUC1DFVxe7T77Dv+pOPuJ7qSfcVUnVtzpLMMWJsDG6NHpy0JhsS9
wVYQgpYWRRZ7bJyfRCJxzIdYF3qy/P9NWyZSlDUuv11s1GSFO2pNd34p59GacVAL
BJE6y5eOPTSbtkmBW/ukaVYdI5NLXNer3IaK3fetV3LvYGOaX8hR45FI1pvyKYvf
S5ol3bQmY1mv78XKkOk=
-----END CERTIFICATE-----"));

        private string _token;
        private string _location;
        private string _accountKey;

        private static bool GetIsValidGuid(string guidString)
        {
            if (string.IsNullOrEmpty(guidString))
                return false;

            Guid newGuid;
            return Guid.TryParse(guidString, out newGuid);
        }

        private static string GetCredentials()
        {
            var configToken = RetrieveSetting(ConfigTokenName);

            if (!string.IsNullOrEmpty(configToken) && GetIsValidGuid(configToken))
            {
                return configToken;
            }

            throw new ConfigurationErrorsException(
                "No LogEntries Token configured make sure your have Logentries.Token in your app.config or cloudconfig.");
        }

        private static string RetrieveSetting(String name)
        {
            var cloudconfig = CloudConfigurationManager.GetSetting(name);

            return String.IsNullOrWhiteSpace(cloudconfig) ? ConfigurationManager.AppSettings[name] : cloudconfig;
        }
        
        private void Run()
        {
            try
            {
                while (true)
                {
                    // Take data from queue.
                    string line;

                    if (this.Queue.TryDequeue(out line))
                    {
                        // Replace newline chars with line separator to format multi-line events nicely.
                        var finalLine = Token + line.Replace(Environment.NewLine, LineSeparator)
                            .Replace("\n", LineSeparator)
                            .Replace("\r", LineSeparator) + "\n";

                        var data = Encoding.UTF8.GetBytes(finalLine);

                        // Send data, reconnect if needed.
                        while (true)
                        {
                            try
                            {
                                this.EnsureOpenConnection();

                                if (!this.Stream.CanWrite)
                                {
                                    this.CloseConnection();
                                    continue;
                                }

                                this.Stream.Write(data, 0, data.Length);

                                if (this.ImmediateFlush)
                                    this.Stream.Flush();
                            }
                            catch (IOException)
                            {
                                continue;
                            }
                            catch (SocketException)
                            {
                                //can't open connection wait a little longer.
                                Thread.Sleep(100);
                                continue;
                            }
                            catch (InvalidOperationException)
                            {
                                continue;
                            }
                            finally
                            {
                                //couldn't write/read to the network
                                Thread.Sleep(1);
                            }
                            break;
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        public AsyncLoggerBase()
        {
            this.Queue = new ConcurrentQueue<string>();

            this.WorkerThread = new Thread(Run)
            {
                Name = "Logentries Logger",
                IsBackground = true
            };
            this.WorkerThread.Start();
        }

        public string Token
        {
            get
            {
                //Make sure we have a token;
                _token = _token ?? GetCredentials();
                return _token;
            }
            set { _token = value; }
        }

        public string Location
        {
            get
            {
                _location = _location ?? RetrieveSetting(ConfigLocation);
                return _location;
            }
            set { _location = value; }
        }

        public string AccountKey
        {
            get
            {
                _accountKey = _accountKey ?? RetrieveSetting(ConfigAccountKey);
                return _accountKey;
            }
            set { _accountKey = value; }
        }

        public bool UseSsl { get; set; }
        public bool ImmediateFlush { get; set; }

        public virtual void AddLine(string line)
        {
            if (Token == null && (AccountKey == null || Location == null))
            {
                throw new ConfigurationErrorsException(
                    "No LogEntries Credentials configured make sure your have \"Logentries.Token\" in your app.config or cloudconfig. Or when you are using HTTP PUT you should have \"Logentries.AccountKey\" and \"Logentries.Location\" specified in your config. Or in the configuration of your logger.");
            }

            this.Queue.Enqueue(line.TrimEnd(TrimChars));
        }

        protected abstract void EnsureOpenConnection();
        protected abstract void CloseConnection();
        public abstract void Dispose();
    }
}
