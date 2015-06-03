namespace LogentriesCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using Microsoft.Azure;

    public enum ForegroundColor
    {
        Black = 30,
        Red = 31,
        Green = 32,
        Yellow = 33,
        Blue = 34,
        Mangenta = 35,
        Cyan = 36,
        White = 37
    }

    public enum BackgroundColor
    {
        Black = 40,
        Red = 41,
        Green = 42,
        Yellow = 44,
        Blue = 44,
        Mangenta = 45,
        Cyan = 46,
        White = 47
    }

    public enum Attribute
    {
        Normal = 0,
        Bold = 1,
        Underline = 4,
        Test = 2
    }

    public class Style
    {
        public Style()
        {
            Attributes = new List<Attribute> { Attribute.Normal };
            BackgroundColor = BackgroundColor.White;
            ForegroundColor = ForegroundColor.Black;
        }

        public static readonly Style Default = new Style ();
        
        public List<Attribute> Attributes { get; set; }

        public BackgroundColor BackgroundColor { get; set; }

        public ForegroundColor ForegroundColor { get; set; }

        public override string ToString()
        {
            return "\x1b[" + (int)ForegroundColor + ";" + (int)BackgroundColor + ";" + Attributes.Aggregate("", (s, attribute) => s == "" ? ((int)attribute).ToString() : s + ";" + (int)attribute) + "m";
        }
    }

    public abstract class AsyncLoggerBase : IAsyncLogger
    {
        // Logentries API server address. 
        protected const string LeApiUrl = "data.logentries.com";

        // New Logentries configuration names.
        private const string ConfigTokenName = "Logentries.Token";

        // New Logentries configuration names.
        private const string ConfigAccountKey = "Logentries.AccountKey";

        // New Logentries configuration names.
        private const string ConfigLocation = "Logentries.LocationName";

        // Secure pot setting
        private const string ConfigSecurePort = "Logentries.SecurePort";

        // Port setting
        private const string ConfigPort = "Logentries.Port";

        // Newline char to trim from message for formatting. 
        private static readonly char[] TrimChars = {'\r', '\n'};

        // Unicode line separator character 
        private const string LineSeparator = "\u2028";

        // Thread that takes care of pushing to the Logentries server
        protected Thread WorkerThread { get; set; }

        // Internal queue of messages to be sent.
        private ConcurrentQueue<Tuple<string, Style>> Queue { get; set; }

        protected Stream Stream;

        // Logentries API server certificate. 
        protected static readonly X509Certificate2 LeApiServerCertificate =
            new X509Certificate2(Encoding.UTF8.GetBytes(
                @"-----BEGIN CERTIFICATE-----
MIIFXjCCBEagAwIBAgIRAN5SEMXHPPIIhx4xrZ5XKAowDQYJKoZIhvcNAQELBQAwgZAxCzAJBgNV
BAYTAkdCMRswGQYDVQQIExJHcmVhdGVyIE1hbmNoZXN0ZXIxEDAOBgNVBAcTB1NhbGZvcmQxGjAY
BgNVBAoTEUNPTU9ETyBDQSBMaW1pdGVkMTYwNAYDVQQDEy1DT01PRE8gUlNBIERvbWFpbiBWYWxp
ZGF0aW9uIFNlY3VyZSBTZXJ2ZXIgQ0EwHhcNMTMwOTMwMDAwMDAwWhcNMTYwOTMwMjM1OTU5WjBW
MSEwHwYDVQQLExhEb21haW4gQ29udHJvbCBWYWxpZGF0ZWQxEzARBgNVBAsTCkNPTU9ETyBTU0wx
HDAaBgNVBAMTE2RhdGEubG9nZW50cmllcy5jb20wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEK
AoIBAQC10Z4dC/BzAtnCIBOiINqS05cGq/DgyF/upGY3vNp9M2zpwMnPGiaDktjtUnX65BT19o3v
2CULw1LrJODxBU6leEjGjckZst72U0WOcX2vvW3WgNuCowYrtxycdZcdIg/2BNS79X5wlT1ZemSw
6rj5HwkjF7c4rHLoh1EcMOqQHvHg9Zi3LC7XSuqmFoteoZZ6IkISIMq0gpD9Pw2hcwi8Xh8u6wMW
jt/spHRMblNYbw36QxcOwAjfiTjcsFMgqjBGO/1ME7CE3uA7V0WD3TSPw5p1BIlsSWkA5YKF51Z+
fiKEl5oX2UwMjpQAq3PN2++XP4v9127gYjCgXv42z0qhAgMBAAGjggHqMIIB5jAfBgNVHSMEGDAW
gBSQr2o6lFoL2JDqElZz30O0Oija5zAdBgNVHQ4EFgQUOTkBTLSVP5hLphrAEG8BrohxObswDgYD
VR0PAQH/BAQDAgWgMAwGA1UdEwEB/wQCMAAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMC
MFAGA1UdIARJMEcwOwYMKwYBBAGyMQECAQMEMCswKQYIKwYBBQUHAgEWHWh0dHBzOi8vc2VjdXJl
LmNvbW9kby5uZXQvQ1BTMAgGBmeBDAECATBUBgNVHR8ETTBLMEmgR6BFhkNodHRwOi8vY3JsLmNv
bW9kb2NhLmNvbS9DT01PRE9SU0FEb21haW5WYWxpZGF0aW9uU2VjdXJlU2VydmVyQ0EuY3JsMIGF
BggrBgEFBQcBAQR5MHcwTwYIKwYBBQUHMAKGQ2h0dHA6Ly9jcnQuY29tb2RvY2EuY29tL0NPTU9E
T1JTQURvbWFpblZhbGlkYXRpb25TZWN1cmVTZXJ2ZXJDQS5jcnQwJAYIKwYBBQUHMAGGGGh0dHA6
Ly9vY3NwLmNvbW9kb2NhLmNvbTA3BgNVHREEMDAughNkYXRhLmxvZ2VudHJpZXMuY29tghd3d3cu
ZGF0YS5sb2dlbnRyaWVzLmNvbTANBgkqhkiG9w0BAQsFAAOCAQEABXgfABRiI3+U3nmkin+UNdMd
7+oPk/pq+caCoAWhcq0koced+egZU2FvinBiKq4oZTEuvbsJ+7BnU18XmlIJHL3Yq9PTD2e6gZ7/
9P18n1TsEMunR1I+QE3M6nK0lsvfdRs7mXfHWkipXEawOxYLd7UT9qhGP/Aoe/QhYhdYfaWZDl2A
+cOjhp8Aq0Vixpva/mPA/qYHAqLtnFYZKEl+LF1ZFO6d6280u4E4kwRGU1XO0NBip1NG2bEh4wsK
RcV+srSS810KCFOkZ+SoInyyPmDUTO5Hokq+nSqNH7SUMMJAeItzt7ydFWTiGAHjvOfg5HityALH
YyQWcJWa+MwmoA==
-----END CERTIFICATE-----"));

        private string _token;
        private string _location;
        private string _accountKey;
        private int _securePort;
        private int _port;
        
        protected AsyncLoggerBase()
        {
            this.Queue = new ConcurrentQueue<Tuple<string, Style>>();

            this.WorkerThread = new Thread(Run)
            {
                Name = "Logentries Logger",
                IsBackground = true
            };
            this.WorkerThread.Start();
        }

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

            return null;
        }

        private static string RetrieveSetting(String name)
        {
            var cloudconfig = CloudConfigurationManager.GetSetting(name);
            if (!String.IsNullOrWhiteSpace(cloudconfig))
            {
                return cloudconfig;
            }

            var appconfig = ConfigurationManager.AppSettings[name];
            if (!String.IsNullOrWhiteSpace(appconfig))
            {
                return appconfig;
            }

            var envconfig = Environment.GetEnvironmentVariable(name);
            if (!String.IsNullOrWhiteSpace(envconfig))
            {
                return envconfig;
            }

            return null;
        }
        
        private void Run()
        {
            try
            {
                while (true)
                {
                    // Take data from queue.
                    Tuple<string, Style> line;

                    if (this.Queue.TryDequeue(out line))
                    {
                        
                        // Replace newline chars with line separator to format multi-line events nicely.
                        var finalLine = Token + line.Item2 + line.Item1.Replace(Environment.NewLine, LineSeparator)
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

        public string LocationName
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

        public int SecurePort
        {
            get
            {
                _securePort = RetrieveSetting(ConfigSecurePort) != null ? int.Parse(RetrieveSetting(ConfigSecurePort)) : _securePort; ;
                return _securePort;
            }
            set { _securePort = value; }
        }

        public int Port
        {
            get
            {
                _port = RetrieveSetting(ConfigPort) != null ? int.Parse(RetrieveSetting(ConfigPort)) : _port;
                return _port;
            }
            set { _port = value; }
        }

        public bool UseSsl { get; set; }
        public bool ImmediateFlush { get; set; }

        public virtual void AddLine(string line, Style style = null)
        {
            if (style == null)
            {
                style = Style.Default;
            }

            if (Token == null && (AccountKey == null || LocationName == null))
            {
                throw new ConfigurationErrorsException(
                    "No LogEntries Credentials configured make sure your have \"Logentries.Token\" in your app.config or cloudconfig. Or when you are using HTTP PUT you should have \"Logentries.AccountKey\" and \"Logentries.LocationName\" specified in your config. Or in the configuration of your logger.");
            }

            this.Queue.Enqueue(Tuple.Create(line.TrimEnd(TrimChars), style));
        }

        protected abstract void EnsureOpenConnection();
        protected abstract void CloseConnection();
        public abstract void Dispose();
    }
}
