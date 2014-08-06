namespace LogentriesCore
{
    using System;

    public interface IAsyncLogger: IDisposable
    {
        bool UseSsl { get; set; }
        bool ImmediateFlush { get; set; }
        string Token { get; set; }
        string Location { get; set; }
        string AccountKey { get; set; }

        void AddLine(string line);
    }
}