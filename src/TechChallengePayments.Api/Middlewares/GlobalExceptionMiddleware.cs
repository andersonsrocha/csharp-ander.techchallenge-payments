﻿using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TechChallengePayments.Api.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<GlobalExceptionMiddleware> logger)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            var errorResponse = $"An unexpected fault happened, please contact yor Administrator with the error id: {correlationId}.";
            
            logger.LogError(ex, "An unexpected fault happened, please contact yor Administrator with the error id: {correlationId}.", correlationId);
            
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            
            var problem = new ProblemDetails
            {
                Title = "An unexpected fault happened",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = errorResponse,
                Instance = context.Request.Path,
            };
            
            var result = JsonSerializer.Serialize(problem);
            await context.Response.WriteAsync(result);
        }
    }
}