using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KeePassAutoReload
{
    internal static class HttpRetryPolicy
    {
        public const int DefaultMaxRetries = 3;

        public static async Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> operation, int maxRetries, CancellationToken cancellationToken = default)
        {
            if (operation == null) throw new ArgumentNullException("operation");
            if (maxRetries < 0) throw new ArgumentOutOfRangeException("maxRetries");

            cancellationToken.ThrowIfCancellationRequested();

            Exception lastException = null;
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    HttpResponseMessage response = await operation();
                    if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
                    {
                        response.Dispose();
                        if (attempt == maxRetries)
                        {
                            throw new HttpRequestException("Server returned " + (int)response.StatusCode + " after " + maxRetries + " retries.");
                        }
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }
                    return response;
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    await DelayAsync(attempt, cancellationToken);
                }
                catch (TaskCanceledException ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    await DelayAsync(attempt, cancellationToken);
                }
            }

            throw lastException ?? new HttpRequestException("Request failed after retries.");
        }

        private static async Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            int milliseconds = (int)(100 * Math.Pow(2, attempt));
            await Task.Delay(milliseconds, cancellationToken);
        }
    }
}
