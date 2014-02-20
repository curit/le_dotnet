namespace log4net.Appender
{
    using System;
    using Core;
    using LogentriesCore;

    public class LogentriesAppender : AppenderSkeleton
    {
        private readonly AsyncLogger _logentriesAsync;

        public LogentriesAppender()
        {
            _logentriesAsync = new AsyncLogger();
        }

        public string Token
        {
            get { return _logentriesAsync.Token; }
            set { _logentriesAsync.Token = value; }
        }

        public bool ImmediateFlush
        {
            get { return _logentriesAsync.ImmediateFlush; }
            set { _logentriesAsync.ImmediateFlush = value; }
        }

        public bool UseSsl
        {
            get { return _logentriesAsync.UseSsl; }
            set { _logentriesAsync.UseSsl = value; }
        }
    
        protected override void Append(LoggingEvent loggingEvent)
        {
            _logentriesAsync.AddLine(RenderLoggingEvent(loggingEvent));
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            Array.ForEach(loggingEvents, Append);
        }

        protected override bool RequiresLayout
        {
            get
            {
                return true;
            }
        }

        protected override void OnClose()
        {
            _logentriesAsync.Dispose();
        }
    }
}
