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

        public string LocationName
        {
            get { return LogentriesAsync.LocationName; }
            set { LogentriesAsync.LocationName = value; }
        }

        public int Port
        {
            get { return LogentriesAsync.Port; }
            set { LogentriesAsync.Port = value; }
        }
        public int SecurePort
        {
            get { return LogentriesAsync.SecurePort; }
            set { LogentriesAsync.SecurePort = value; }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var renderedEvent = Layout.Render(logEvent);
            _logentriesAsync.AddLine(renderedEvent);
        }

        protected override void CloseTarget()
        {
            base.CloseTarget();

            _logentriesAsync.Dispose();
        }
    }
}
