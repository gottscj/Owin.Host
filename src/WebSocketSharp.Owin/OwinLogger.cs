using System;
using System.Diagnostics;
using System.Globalization;

namespace WebSocketSharp.Owin
{
	internal class OwinLogger : ILogger
	{
		private static readonly Func<object, Exception, string> _logState =
			(state, error) => Convert.ToString(state, CultureInfo.CurrentCulture);

		private static readonly Func<object, Exception, string> _logStateAndError =
			(state, error) => string.Format(CultureInfo.CurrentCulture, "{0}\r\n{1}", state, error);

		private readonly Func<TraceEventType, int, object, Exception, Func<object, Exception, string>, bool> _logger;

		public OwinLogger(Func<string, Func<TraceEventType, int, object, Exception, Func<object, Exception, string>, bool>> factory, Type type)
		{
			_logger = factory(type.FullName);
		}

		public void Info(string data)
		{
			if (_logger == null)
			{
				System.Diagnostics.Debug.WriteLine(data);
				return;
			}

			_logger(TraceEventType.Information, 0, data, null, _logState);
		}

		public void Exception(string location, Exception exception)
		{
			if (_logger == null)
			{
				System.Diagnostics.Debug.WriteLine(exception);
				return;
			}

			_logger(TraceEventType.Error, 0, location, exception, _logStateAndError);
		}

		public void Debug(string format, params object[] args)
		{
			
			var data = string.Format(format, args);
			System.Diagnostics.Debug.WriteLine(data);
		}

		public void Info(string format, params object[] args)
		{
			Info(string.Format(format, args));
		}

		public void Warn(string format, params object[] args)
		{
			var data = string.Format(format, args);
			if (_logger == null)
			{
				System.Diagnostics.Debug.WriteLine(data);
				return;
			}
			_logger(TraceEventType.Warning, 0, data, null, _logState);
		}

		public void Error(string format, params object[] args)
		{
			var data = string.Format(format, args);
			if (_logger == null)
			{
				System.Diagnostics.Debug.WriteLine(data);
				return;
			}
			_logger(TraceEventType.Error, 0, data, null, _logStateAndError);
		}

		public void ErrorException(string message, Exception exception, string location)
		{
			if (_logger == null)
			{
				System.Diagnostics.Debug.WriteLine(message);
				return;
			}
			_logger(TraceEventType.Error, 0, location, exception, _logStateAndError);
		}

		public void ErrorException(string message, Exception exception)
		{
			if (_logger == null)
			{
				System.Diagnostics.Debug.WriteLine(message);
				return;
			}
			_logger(TraceEventType.Error, 0, message, exception, _logStateAndError);
		}
	}
}