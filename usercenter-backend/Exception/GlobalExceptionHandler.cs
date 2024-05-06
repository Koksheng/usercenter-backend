using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using usercenter_backend.Common;

namespace usercenter_backend.Exception
{
    public static class GlobalExceptionHandler
    {
        //private readonly ILogger<GlobalExceptionHandler> _logger;

        //public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        //{
        //    _logger = logger;
        //}

        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature =
                        context.Features.Get<IExceptionHandlerPathFeature>();

                    var exception = exceptionHandlerPathFeature?.Error;

                    if (exception is BusinessException businessException)
                    {
                        //_logger.LogError(exception, "BusinessException: {Message}", businessException.Message);

                        var response = ResultUtils.error<object>(businessException.Code, null, businessException.Message, businessException.Description);

                        //var response = new BaseResponse<object>(businessException.Code, null, businessException.Message);
                        context.Response.StatusCode = 200; // Bad Request
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                    }
                    else
                    {
                        //_logger.LogError(exception, "Unhandled Exception");

                        var response = new BaseResponse<object>(500, null, "Internal Server Error");
                        context.Response.StatusCode = 500; // Internal Server Error
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                    }
                });
            });
        }
    }
}
