﻿namespace LogentriesCore
{
    using System;
    using System.Net.Security;
    using System.Net.Sockets;

    public class AsyncTcpLogger : AsyncLoggerBase
    {
        public AsyncTcpLogger()
        {
            Port = 10000;
            SecurePort = 20000;
        }

        private TcpClient _leClient;

        protected override void EnsureOpenConnection()
        {
            if (_leClient != null && _leClient.Connected) return;
            
            _leClient = new TcpClient(LeApiUrl, (UseSsl ? SecurePort : Port)) { NoDelay = true };

            if (UseSsl)
            {
                Stream = new SslStream(_leClient.GetStream(), false,
                    (sender, cert, chain, errors) =>
                        cert.GetCertHashString() == LeApiServerCertificate.GetCertHashString());
                //posible authentication exceptions aren't caught because we want to see them in the eventwvr
                ((SslStream) Stream).AuthenticateAsClient(LeApiUrl);
            }
            else
            {
                Stream = _leClient.GetStream();
            }
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
                    this.WorkerThread.Abort();
                }
            }
        }
    }
}