using System;

namespace WebSocketSharp.Owin
{
    public interface ILogger
    {
        void Debug(string format, params object[] args);
        void Info(string format, params object[] args);

        void Warn(string format, params object[] args);
        void Error(string format, params object[] args);

        void ErrorException(string message, Exception exception, string location);
        void ErrorException(string message, Exception exception);
    }
}