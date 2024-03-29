using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading;
using WebSocketSharp.Owin.RequestProcessing;
using HttpListenerContext = Gottscj.Owin.Host.SocketHttpListener.Net.HttpListenerContext;
using HttpListenerResponse = Gottscj.Owin.Host.SocketHttpListener.Net.HttpListenerResponse;
using HttpStatusCode = Gottscj.Owin.Host.SocketHttpListener.Net.HttpStatusCode;

namespace Gottscj.Owin.Host.RequestProcessing
{
    /// <summary>
    /// This wraps an HttpListenerResponse, populates it with the given response fields, and relays 
    /// the response body to the underlying stream.
    /// </summary>
    internal class OwinHttpListenerResponse
    {
        private const int RequestInProgress = 1;
        private const int ResponseInProgress = 2;
        private const int Completed = 3;

        private static readonly Action<object> SResponseBodyStarted = OnResponseBodyStarted;

        private readonly CallEnvironment _environment;
        private readonly HttpListenerResponse _response;

        private readonly HttpListenerContext _context;
        private bool _responsePrepared;
        private IList<Tuple<Action<object>, object>> _onSendingHeadersActions;
        private int _requestState;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwinHttpListenerResponse"/> class.
        /// Sets up the Environment with the necessary request state items.
        /// </summary>
        internal OwinHttpListenerResponse(HttpListenerContext context, CallEnvironment environment)
        {
            _context = context;
            _response = _context.Response;
            _environment = environment;

            _requestState = RequestInProgress;

            // Provide the default status code for consistency with SystemWeb, even though it's optional.
            _environment.ResponseStatusCode = (int)HttpStatusCode.Ok; // 200

            var outputStream = new HttpListenerStreamWrapper(_response.OutputStream);
            outputStream.OnFirstWrite(SResponseBodyStarted, this);
            _environment.ResponseBody = outputStream;

            _environment.ResponseHeaders = new ResponseHeadersDictionary(_response);

            _onSendingHeadersActions = new List<Tuple<Action<object>, object>>();
            _environment.OnSendingHeaders = RegisterForOnSendingHeaders;
        }

        internal bool TryStartResponse()
        {
            return Interlocked.CompareExchange(ref _requestState, ResponseInProgress, RequestInProgress) == RequestInProgress;
        }

        internal bool TryFinishResponse()
        {
            return Interlocked.CompareExchange(ref _requestState, Completed, ResponseInProgress) == ResponseInProgress;
        }

        private static void OnResponseBodyStarted(object state)
        {
            OwinHttpListenerResponse thisPtr = (OwinHttpListenerResponse)state;
            thisPtr.ResponseBodyStarted();
        }

        private void ResponseBodyStarted()
        {
            PrepareResponse(mayHaveBody: true);

            if (!TryStartResponse())
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
        
        internal void CompleteResponse()
        {
            PrepareResponse(mayHaveBody: false);
        }
        
        // The request completed successfully.
        public void Close()
        {
            TryStartResponse();

            if (TryFinishResponse())
            {
                //_context.Response.Close();
                _context.Response.Close();
            }
        }

        // Set the status code and reason phrase from the environment.
        private void PrepareResponse(bool mayHaveBody)
        {
            if (_responsePrepared)
            {
                return;
            }

            _responsePrepared = true;

            NotifyOnSendingHeaders();

            SetStatusCode();

            SetReasonPhrase();

            // response.ProtocolVersion is ignored by Http.Sys.  It always sends 1.1

            // Default to Content-Length: 0 rather than chunked if there's no body (unless otherwise specified).
            // Note that setting it to 0 is required even when it's already 0.
            /*if (!mayHaveBody && !_response.SendChunked && _response.ContentLength64 <= 0)
            {
                _response.ContentLength64 = 0;
            }*/
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "By design")]
        private void SetStatusCode()
        {
            int statusCode = _environment.ResponseStatusCode;
            // Default / not present
            if (statusCode != 0)
            {
                if (statusCode == 100 || statusCode < 100 || statusCode >= 1000)
                {
                    throw new ArgumentOutOfRangeException(Constants.ResponseStatusCodeKey, statusCode, string.Empty);
                }

                _response.StatusCode = statusCode;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "By design")]
        private void SetReasonPhrase()
        {
            string reasonPhrase = _environment.ResponseReasonPhrase;
            if (!string.IsNullOrWhiteSpace(reasonPhrase))
            {
                _response.StatusDescription = reasonPhrase;
            }
        }

        private void RegisterForOnSendingHeaders(Action<object> callback, object state)
        {
            IList<Tuple<Action<object>, object>> actions = _onSendingHeadersActions;
            if (actions == null)
            {
                throw new InvalidOperationException("Headers already sent");
            }

            actions.Add(new Tuple<Action<object>, object>(callback, state));
        }

        private void NotifyOnSendingHeaders()
        {
            IList<Tuple<Action<object>, object>> actions = Interlocked.Exchange(ref _onSendingHeadersActions, null);
            Contract.Assert(actions != null);

            // Execute last to first. This mimics a stack unwind.
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                Tuple<Action<object>, object> actionPair = actions[i];
                actionPair.Item1(actionPair.Item2);
            }
        }

        internal void End()
        {
            int priorState = Interlocked.Exchange(ref _requestState, Completed);

            if (priorState == RequestInProgress)
            {
                // Premature ending, must be an error.
                // If the response has not started yet then we can send an error response before closing it.
                _context.Response.StatusCode = 500;
                _context.Response.ContentLength64 = 0;
                _context.Response.Headers.Clear();
                try
                {
                    //_context.Response.Close();
                    _context.Response.Close();
                }
                catch (HttpListenerException)
                {
                    // TODO: Trace
                }
            }
            else if (priorState == ResponseInProgress)
            {
                _context.Response.Abort();
            }
            else
            {
                Contract.Requires(priorState == Completed);

                // Clean up after exceptions in the shutdown process. No-op if Response.Close() succeeded.
                _context.Response.Abort();
            }
        }

    }
}