namespace LogentriesCore
{
    using System;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Text;

    public class AsyncHttpLogger : AsyncLoggerBase
    {
        // Port number for HTTP PUT logging on Logentries API server. 
        private const int LeApiHttpPort = 80;

        // Port number for SSL HTTP PUT logging on Logentries API server. 
        private const int LeApiHttpsPort = 443;

        private TcpClient _leClient;
        
        protected override void EnsureOpenConnection()
        {
            if (_leClient != null && _leClient.Connected) return;

            _leClient = new TcpClient(LeApiUrl, (UseSsl ? LeApiHttpPort : LeApiHttpsPort)) { NoDelay = true };

            if (UseSsl)
            {
                Stream = new SslStream(_leClient.GetStream(), false, (sender, cert, chain, errors) => cert.GetCertHashString() == LeApiServerCertificate.GetCertHashString());
                //posible authentication exceptions aren't caught because we want to see them in the eventwvr
                ((SslStream)Stream).AuthenticateAsClient(LeApiUrl);
            }

            Stream = _leClient.GetStream();

            var header = String.Format("PUT /{0}/hosts/{1}/?realtime=1 HTTP/1.1\r\n\r\n", AccountKey, Location);
            Stream.Write(Encoding.ASCII.GetBytes(header), 0, header.Length);
        }

        protected override void CloseConnection()
        {
            _leClient.Close();
        }

        public override void Dispose()
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
                if (_leClient != null)
                {
                    _leClient.Close();
                }
            }
        }
    }
}