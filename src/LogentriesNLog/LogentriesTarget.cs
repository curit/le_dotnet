namespace NLog.Targets
{
    using LogentriesCore;

    [Target("Logentries")]
    public sealed class LogentriesTarget : TargetWithLayout
    {
        private readonly AsyncLogger _logentriesAsync;

        public LogentriesTarget()
        {
            _logentriesAsync = new AsyncLogger();
        }

        /** Option to set Token programmatically or in Appender Definition */
        public string Token
        {
            get { return _logentriesAsync.Token; }
            set { _logentriesAsync.Token = value; }
        }

        /** SSL/TLS parameter flag */
        public bool UseSsl
        {
            get { return _logentriesAsync.UseSsl; }
            set { _logentriesAsync.UseSsl = value; }
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
