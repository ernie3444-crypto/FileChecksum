using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileChecksum.Core.Abstractions;

namespace FileChecksum.Core.Services
{
    /// <summary>
    /// HTTP server that exposes Prometheus metrics on a /metrics endpoint.
    /// Handles metric requests asynchronously without blocking the service.
    /// </summary>
    public class MetricsHttpServer : IDisposable
    {
        private readonly int _port;
        private readonly IMetricsCollector _metricsCollector;
        private HttpListener _httpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenerTask;

        public MetricsHttpServer(int port, IMetricsCollector metricsCollector)
        {
            if (port < 1 || port > 65535)
            {
                throw new ArgumentException($"Invalid port: {port}", nameof(port));
            }

            _port = port;
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        }

        /// <summary>
        /// Starts the HTTP listener on the configured port.
        /// </summary>
        public void Start()
        {
            if (_httpListener != null)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://+:{_port}/");
            _httpListener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            _listenerTask = HandleRequests(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops the HTTP listener.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();

            if (_httpListener != null)
            {
                try
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }
                catch { }
                finally
                {
                    _httpListener = null;
                }
            }

            try
            {
                _listenerTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
        }

        private async Task HandleRequests(CancellationToken cancellationToken)
        {
            while (_httpListener != null && _httpListener.IsListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    // Fire and forget - handle each request without blocking the listener
                    _ = HandleRequest(context, cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // Expected when listener is stopped
                    break;
                }
                catch (Exception)
                {
                    // Log but continue listening
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (context.Request.Url.AbsolutePath == "/metrics")
                {
                    await HandleMetricsRequest(context);
                }
                else
                {
                    SendResponse(context, 404, "Not Found");
                }
            }
            catch (Exception)
            {
                try
                {
                    SendResponse(context, 500, "Internal Server Error");
                }
                catch { }
            }
        }

        private async Task HandleMetricsRequest(HttpListenerContext context)
        {
            string metrics = _metricsCollector.ExportMetrics();
            byte[] buffer = Encoding.UTF8.GetBytes(metrics);

            context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = 200;

            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private void SendResponse(HttpListenerContext context, int statusCode, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = statusCode;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}
