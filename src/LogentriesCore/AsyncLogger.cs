namespace LogentriesCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Configuration;
    using System.IO;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using Microsoft.WindowsAzure;

    public class AsyncLogger : IDisposable
    {
        // Logentries API server address. 
        private const String LeApiUrl = "api.logentries.com";

        // Port number for token logging on Logentries API server. 
        private const int LeApiTokenPort = 10000;

        // Port number for TLS encrypted token logging on Logentries API server 
        private const int LeApiTokenTlsPort = 20000;

        // New Logentries configuration names.
        private const String ConfigTokenName = "Logentries.Token";

        // Newline char to trim from message for formatting. 
        private static readonly char[] TrimChars = { '\r', '\n' };

        /** Unicode line separator character */
        private const string LineSeparator = "\u2028";

        public AsyncLogger()
        {
            _queue = new ConcurrentQueue<string>();

            WorkerThread = new Thread(Run)
            {
                Name = "Logentries Log4net Appender", 
                IsBackground = true
            };
            WorkerThread.Start();
        }

        private Thread WorkerThread { get; set; }

        public string Token { get; set; }

        public bool UseSsl { get; set; }

        public bool ImmediateFlush { get; set; }

        private readonly ConcurrentQueue<string> _queue;
        
        private TcpClient _leClient;

        protected Stream Stream;
        
        private void Run()
        {
            try
            {
                while (true)
                {
                    // Take data from queue.
                    string line;

                    if (_queue.TryDequeue(out line)) { 
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
                                EnsureOpenConnection();
                                Stream.Write(data, 0, data.Length);

                                if (ImmediateFlush)
                                    Stream.Flush();
                            }
                            catch (IOException)
                            {
                                Thread.Sleep(1);
                                continue;
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

        private string RetrieveSetting(String name)
        {
            var cloudconfig = CloudConfigurationManager.GetSetting(name);

            return String.IsNullOrWhiteSpace(cloudconfig) ? ConfigurationManager.AppSettings[name] : cloudconfig;
        }

        private string GetCredentials()
        {
            var configToken = RetrieveSetting(ConfigTokenName);

            if (!String.IsNullOrEmpty(configToken) && GetIsValidGuid(configToken))
            {
                return configToken;
            }

            throw new ConfigurationErrorsException("No LogEntries Token configured make sure your have Logentries.Token in your app.config or cloudconfig.");
        }


        private bool GetIsValidGuid(string guidString)
        {
            if (String.IsNullOrEmpty(guidString))
                return false;

            Guid newGuid;
            return Guid.TryParse(guidString, out newGuid);
        }

        public void AddLine(string line)
        {
            EnsureToken();
            _queue.Enqueue(line.TrimEnd(TrimChars));
        }

        private void EnsureToken()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                Token = GetCredentials();
            }
        }

        protected virtual void EnsureOpenConnection()
        {
            if (_leClient == null || !_leClient.Connected)
            {
                _leClient = new TcpClient(LeApiUrl, (UseSsl ? LeApiTokenTlsPort : LeApiTokenPort))
                {
                    NoDelay = true
                };
                
                if (UseSsl)
                {
                    Stream = new SslStream(_leClient.GetStream(), false, (sender, cert, chain, errors) => cert.GetCertHashString() == LeApiServerCertificate.GetCertHashString());
                    ((SslStream)Stream).AuthenticateAsClient(LeApiUrl);
                }

                Stream = _leClient.GetStream();
            }
        }
      
        // Logentries API server certificate. 
        private static readonly X509Certificate2 LeApiServerCertificate =
            new X509Certificate2(Encoding.UTF8.GetBytes(
                @"-----BEGIN CERTIFICATE-----
                MIIFSjCCBDKgAwIBAgIDBQMSMA0GCSqGSIb3DQEBBQUAMGExCzAJBgNVBAYTAlVT
                MRYwFAYDVQQKEw1HZW9UcnVzdCBJbmMuMR0wGwYDVQQLExREb21haW4gVmFsaWRh
                dGVkIFNTTDEbMBkGA1UEAxMSR2VvVHJ1c3QgRFYgU1NMIENBMB4XDTEyMDkxMDE5
                NTI1N1oXDTE2MDkxMTIxMjgyOFowgcExKTAnBgNVBAUTIEpxd2ViV3RxdzZNblVM
                ek1pSzNiL21hdktiWjd4bEdjMRMwEQYDVQQLEwpHVDAzOTM4NjcwMTEwLwYDVQQL
                EyhTZWUgd3d3Lmdlb3RydXN0LmNvbS9yZXNvdXJjZXMvY3BzIChjKTEyMS8wLQYD
                VQQLEyZEb21haW4gQ29udHJvbCBWYWxpZGF0ZWQgLSBRdWlja1NTTChSKTEbMBkG
                A1UEAxMSYXBpLmxvZ2VudHJpZXMuY29tMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A
                MIIBCgKCAQEAxcmFqgE2p6+N9lM2GJhe8bNUO0qmcw8oHUVrsneeVA66hj+qKPoJ
                AhGKxC0K9JFMyIzgPu6FvuVLahFZwv2wkbjXKZLIOAC4o6tuVb4oOOUBrmpvzGtL
                kKVN+sip1U7tlInGjtCfTMWNiwC4G9+GvJ7xORgDpaAZJUmK+4pAfG8j6raWgPGl
                JXo2hRtOUwmBBkCPqCZQ1mRETDT6tBuSAoLE1UMlxWvMtXCUzeV78H+2YrIDxn/W
                xd+eEvGTSXRb/Q2YQBMqv8QpAlarcda3WMWj8pkS38awyBM47GddwVYBn5ZLEu/P
                DiRQGSmLQyFuk5GUdApSyFETPL6p9MfV4wIDAQABo4IBqDCCAaQwHwYDVR0jBBgw
                FoAUjPTZkwpHvACgSs5LdW6gtrCyfvwwDgYDVR0PAQH/BAQDAgWgMB0GA1UdJQQW
                MBQGCCsGAQUFBwMBBggrBgEFBQcDAjAdBgNVHREEFjAUghJhcGkubG9nZW50cmll
                cy5jb20wQQYDVR0fBDowODA2oDSgMoYwaHR0cDovL2d0c3NsZHYtY3JsLmdlb3Ry
                dXN0LmNvbS9jcmxzL2d0c3NsZHYuY3JsMB0GA1UdDgQWBBRaMeKDGSFaz8Kvj+To
                j7eMOtT/zTAMBgNVHRMBAf8EAjAAMHUGCCsGAQUFBwEBBGkwZzAsBggrBgEFBQcw
                AYYgaHR0cDovL2d0c3NsZHYtb2NzcC5nZW90cnVzdC5jb20wNwYIKwYBBQUHMAKG
                K2h0dHA6Ly9ndHNzbGR2LWFpYS5nZW90cnVzdC5jb20vZ3Rzc2xkdi5jcnQwTAYD
                VR0gBEUwQzBBBgpghkgBhvhFAQc2MDMwMQYIKwYBBQUHAgEWJWh0dHA6Ly93d3cu
                Z2VvdHJ1c3QuY29tL3Jlc291cmNlcy9jcHMwDQYJKoZIhvcNAQEFBQADggEBAAo0
                rOkIeIDrhDYN8o95+6Y0QhVCbcP2GcoeTWu+ejC6I9gVzPFcwdY6Dj+T8q9I1WeS
                VeVMNtwJt26XXGAk1UY9QOklTH3koA99oNY3ARcpqG/QwYcwaLbFrB1/JkCGcK1+
                Ag3GE3dIzAGfRXq8fC9SrKia+PCdDgNIAFqe+kpa685voTTJ9xXvNh7oDoVM2aip
                v1xy+6OfZyGudXhXag82LOfiUgU7hp+RfyUG2KXhIRzhMtDOHpyBjGnVLB0bGYcC
                566Nbe7Alh38TT7upl/O5lA29EoSkngtUWhUnzyqYmEMpay8yZIV4R9AuUk2Y4HB
                kAuBvDPPm+C0/M4RLYs=
                -----END CERTIFICATE-----"));


        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass 
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream.Dispose();
                _leClient.Close();
            }
        }
    }
}