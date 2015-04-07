namespace LogentriesCore
{
    using System;

    public interface IAsyncLogger : IDisposable
    {
        bool UseSsl { get; set; }
        bool ImmediateFlush { get; set; }
        string Token { get; set; }
        string LocationName { get; set; }
        string AccountKey { get; set; }
        int SecurePort { get; set; }
        int Port { get; set; }
        void AddLine(string line, Style style = null);
    }
}