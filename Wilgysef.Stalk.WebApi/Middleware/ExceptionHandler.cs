using Microsoft.AspNetCore.Diagnostics;
using System.Net.Mime;
using System.Net;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.WebApi.Middleware;

public class ExceptionHandler
{
    private const string CodeKey = "code";
    private const string MessageKey = "message";
    private const string ExceptionKey = "exception";

    public bool ExceptionsInResponse { get; set; }

    public ExceptionHandlerOptions GetExceptionHandlerOptions()
    {
        return new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = true,
            ExceptionHandler = HandleAsync,
        };
    }

    private async Task HandleAsync(HttpContext context)
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionHandler == null)
        {
            return;
        }

        context.Response.ContentType = MediaTypeNames.Application.Json;

        var responseObject = new Dictionary<string, object>();

        switch (exceptionHandler.Error)
        {
            case EntityNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
            case BusinessException businessException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                responseObject.Add(CodeKey, businessException.Code);
                responseObject.Add(MessageKey, businessException.Message);
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        if (ExceptionsInResponse)
        {
            responseObject.Add(ExceptionKey, exceptionHandler.Error.ToString());
        }

        await context.Response.WriteAsJsonAsync(responseObject);
    }
}
