using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace QuanLiKhachSan.Filters
{
    public class ViewModelLoggingFilter : IResultFilter
    {
        private readonly ILogger<ViewModelLoggingFilter> _logger;

        public ViewModelLoggingFilter(ILogger<ViewModelLoggingFilter> logger)
        {
            _logger = logger;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            try
            {
                if (context.Result is ViewResult vr)
                {
                    var viewName = vr.ViewName ?? context.RouteData.Values["action"]?.ToString() ?? "(unknown)";
                    if (viewName.IndexOf("dashboard", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var modelType = vr.Model?.GetType().FullName ?? "null";
                        var path = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                        _logger.LogInformation("[ViewModelLogging] Rendering view {ViewName} for request {Path} with model type {ModelType}", viewName, path, modelType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ViewModelLoggingFilter");
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            // no-op
        }
    }
}
