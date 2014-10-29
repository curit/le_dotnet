namespace log4net.Appender
{
    using System;
    using System.Collections.Generic;
    using Core;
    using LogentriesCore;
    using Util;
    using Attribute = LogentriesCore.Attribute;

    public class LogentriesAppender : AppenderSkeleton
    {
        public LogentriesAppender()
        {
            LevelMapping = new LevelMapping();
        }

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

        private LevelMapping LevelMapping { get; set; }
        
        /// <summary>
        /// Add a color mapping
        /// </summary>
        /// <param name="mapping">
        /// The mapping
        /// </param>
        public void AddMapping(LevelColors mapping)
        {
            LevelMapping.Add(mapping);
        }


        /// <summary>
        /// Initialize the options for this appender
        /// </summary>
        /// <remarks>
        /// <para>
        /// Initialize the level to color mappings set on this appender.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {
            base.ActivateOptions();
            LevelMapping.ActivateOptions();
        }

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

        protected override void Append(LoggingEvent loggingEvent)
        {
            var levelColors = LevelMapping.Lookup(loggingEvent.Level) as LevelColors;
            if (levelColors != null)
            {
                LogentriesAsync.AddLine(RenderLoggingEvent(loggingEvent), levelColors.ToStyle());
                return;
            }

            LogentriesAsync.AddLine(RenderLoggingEvent(loggingEvent));
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
            if (_logentriesAsync != null) 
                _logentriesAsync.Dispose();
        }

        /// <summary>
        /// A class to act as a mapping between the level that a logging call is made at and
        /// the color it should be displayed as.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Defines the mapping between a level and the color it should be displayed in.
        /// </para>
        /// </remarks>
        public class LevelColors : LevelMappingEntry
        {
            public LevelColors()
            {
                Attributes = new List<Attribute>();
            }

            /// <summary>
            /// The mapped foreground color for the specified level
            /// </summary>
            /// <remarks>
            /// <para>
            /// Required property.
            /// The mapped foreground color for the specified level.
            /// </para>
            /// </remarks>
            public ForegroundColor ForeColor { get; set; }
            
            /// <summary>
            /// The mapped background color for the specified level
            /// </summary>
            /// <remarks>
            /// <para>
            /// Required property.
            /// The mapped background color for the specified level.
            /// </para>
            /// </remarks>
            public BackgroundColor BackColor { get; set; }

            public void AddAttribute(Attribute attr)
            {
                Attributes.Add(attr);
            }

            private List<Attribute> Attributes { get; set; }

            internal Style ToStyle()
            {
                return new Style
                {
                    Attributes = Attributes,
                    ForegroundColor = ForeColor,
                    BackgroundColor = BackColor
                };
            }
        }
    }
}
