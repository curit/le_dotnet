namespace NLog.Targets
{
    using LogentriesCore;

    [Target("Logentries")]
    public sealed class LogentriesTarget : TargetWithLayout
    {
        private IAsyncLogger _logentriesAsync;


        private IAsyncLogger LogentriesAsync
        {
            get
            {
                _logentriesAsync = _logentriesAsync ?? (UseHttpPut ? (IAsyncLogger)new AsyncHttpLogger() : new AsyncTcpLogger());
                return _logentriesAsync;
            }
        }

        public bool UseHttpPut { get; set; }

        public string Token
        {
            get { return LogentriesAsync.Token; }
            set { LogentriesAsync.Token = value; }
        }

        public bool ImmediateFlush
        {
            get { return LogentriesAsync.ImmediateFlush; }
            set { LogentriesAsync.ImmediateFlush = value; }
        }

        public bool UseSsl
        {
            get { return LogentriesAsync.UseSsl; }
            set { LogentriesAsync.UseSsl = value; }
        }

        public string AccountKey
        {
            get { return LogentriesAsync.AccountKey; }
            set { LogentriesAsync.AccountKey = value; }
        }

        public string Location
        {
            get { return LogentriesAsync.Location; }
            set { LogentriesAsync.Location = value; }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var renderedEvent = Layout.Render(logEvent);

            try
            {
                //NLog can pass null references of Exception
                if (logEvent.Exception != null)
                {
                    var excep = logEvent.Exception.ToString();
                    if (excep.Length > 0)
                    {
                        renderedEvent += ", ";
                        renderedEvent += excep;
                    }
                }
            }
            catch { }

            _logentriesAsync.AddLine(renderedEvent);
        }

        protected override void CloseTarget()
        {
            base.CloseTarget();

            _logentriesAsync.Dispose();
        }
    }
}
