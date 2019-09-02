using System;
using System.Diagnostics;
using System.Globalization;

namespace Shure.Cwb.WebApi.Service.OwinHttpServer
{
	internal class OwinLogger
	{
		private static readonly Func<object, Exception, string> LogState =
			(state, error) => Convert.ToString(state, CultureInfo.CurrentCulture);

		private static readonly Func<object, Exception, string> LogStateAndError =
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
				Debug.WriteLine(data);
				return;
			}

			_logger(TraceEventType.Information, 0, data, null, LogState);
		}

		public void Exception(string location, Exception exception)
		{
			if (_logger == null)
			{
				Debug.WriteLine(exception);
				return;
			}

			_logger(TraceEventType.Error, 0, location, exception, LogStateAndError);
		}
	}
}