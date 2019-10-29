using System;

namespace WebSocketSharp.Owin
{
    public class NullLogger : ILogger
    {
        public void Debug(string message)
        {
            
        }

        public void Info(string message)
        {
            
        }

        public void Error(string message)
        {
            
        }

        public void Debug(string format, params object[] args)
        {
        }

        public void Info(string format, params object[] args)
        {
        }

        public void Warn(string format, params object[] args)
        {
            
        }

        public void Error(string format, params object[] args)
        {
        }

        public void ErrorException(string message, Exception exception, string location)
        {
        }

        public void ErrorException(string message, Exception exception)
        {
            
        }
    }
}