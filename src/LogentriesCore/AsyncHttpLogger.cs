namespace LogentriesCore
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Text;

    public class AsyncHttpLogger : AsyncLoggerBase
    {
        private WebClient _leClient;

        public AsyncHttpLogger()
        {
            _leClient = new WebClient();
        }

        protected override void EnsureOpenConnection()
        {
            
        }

        public override void AddLine(string line, Style style = null)
        {
            _leClient.UploadStringAsync(new Uri((UseSsl ? "https://" : "http://") + LeJsApiUrl + "/v1/logs/" + Token), "POST", style.ToString() + line.Replace(Environment.NewLine, LineSeparator));
        }

        protected override void CloseConnection()
        {
            _leClient.Dispose();
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
                    _leClient.Dispose();
                    this.WorkerThread.Abort();
                }
            }
        }
    }
}