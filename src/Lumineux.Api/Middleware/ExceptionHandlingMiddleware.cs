using FluentValidation;
using Lumineux.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Middleware;

/// <summary>
/// Traduit les exceptions en réponses ProblemDetails (RFC 7807) homogènes et non fuitantes
/// (Constitution V). Les messages métier sont explicites ; les erreurs inattendues sont masquées.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title, detail) = Map(ex);

            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Erreur non gérée");
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
            };

            if (ex is DuplicateMemberException dup)
            {
                problem.Extensions["code"] = dup.Code;
                problem.Extensions["duplicateMemberIds"] = dup.DuplicateMemberIds;
            }

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
        }
    }

    private static (int Status, string Title, string Detail) Map(Exception ex) => ex switch
    {
        ValidationException v => (StatusCodes.Status400BadRequest, "Requête invalide",
            string.Join(" ", v.Errors.Select(e => e.ErrorMessage))),
        NotFoundException => (StatusCodes.Status404NotFound, "Ressource introuvable", ex.Message),
        DuplicateMemberException => (StatusCodes.Status409Conflict, "Conflit", ex.Message),
        ConflictException => (StatusCodes.Status409Conflict, "Conflit", ex.Message),
        GoneException => (StatusCodes.Status410Gone, "Expiré", ex.Message),
        ForbiddenException => (StatusCodes.Status403Forbidden, "Accès refusé", ex.Message),
        DomainException => (StatusCodes.Status400BadRequest, "Requête invalide", ex.Message),
        _ => (StatusCodes.Status500InternalServerError, "Erreur interne", "Une erreur inattendue est survenue."),
    };
}
