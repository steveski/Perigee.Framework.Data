﻿namespace Perigee.Framework.Web.Middleware.Logging
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    // http://www.sulhome.com/blog/10/log-asp-net-core-request-and-response-using-middleware
    public class LogRequestMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private Func<object, Exception, string> _defaultFormatter = (state, exception) => state.ToString();

        public LogRequestMiddleware(RequestDelegate next) //, ILogger logger)
        {
            this._next = next;
            //_logger = logger;
        }

        public async Task Invoke(HttpContext context, CancellationToken cancellationToken)
        {
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;

            await context.Request.Body.CopyToAsync(requestBodyStream, cancellationToken).ConfigureAwait(false);
            requestBodyStream.Seek(0, SeekOrigin.Begin);

            //var url = UriHelper.GetDisplayUrl(context.Request);
            var requestBodyText = new StreamReader(requestBodyStream).ReadToEndAsync().ConfigureAwait(false);
            //_logger.Log(LogLevel.Information, 1, $"REQUEST METHOD: {context.Request.Method}, REQUEST BODY: {requestBodyText}, REQUEST URL: {url}", null, _defaultFormatter);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await _next(context).ConfigureAwait(false);
            context.Request.Body = originalRequestBody;
        }
    }
}