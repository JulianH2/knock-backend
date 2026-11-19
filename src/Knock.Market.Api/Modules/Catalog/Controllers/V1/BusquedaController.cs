using System.Globalization;
using Asp.Versioning;
using FluentValidation;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Catalog.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/busqueda")]
[Authorize]
internal sealed class BusquedaController(BusquedaService service, IValidator<BusquedaQuery> validator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<BusquedaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get([FromQuery] BusquedaQueryParameters parameters, CancellationToken cancellationToken)
    {
        // The raw collection, not the bound values, preserves repeated parameters for validation.
        _ = parameters;
        var query = new BusquedaQuery(Request.Query);
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
            var problem = new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "One or more validation errors occurred." };
            problem.Extensions["code"] = ErrorCodes.ValidationFailedCode;
            problem.Extensions["errors"] = errors;
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
                .WriteAsync(new ProblemDetailsContext { HttpContext = HttpContext, ProblemDetails = problem });
            return new EmptyResult();
        }
        var limit = query.Get("limit") is { } value ? int.Parse(value, CultureInfo.InvariantCulture) : 20;
        var verified = query.Get("soloVerificados") is { } flag && bool.Parse(flag);
        var result = await service.SearchAsync(new SearchInput(query.Get("q")!, query.Get("coloniaId")!, query.Get("oficio"), verified, limit, query.Get("cursor")), cancellationToken);
        if (result.Error is not null)
        {
            await SearchProblems.WriteAsync(HttpContext, result.Error);
            return new EmptyResult();
        }
        Response.Headers.CacheControl = "private, max-age=30";
        return Ok(BusquedaResponse.From(result.Response!));
    }
}
