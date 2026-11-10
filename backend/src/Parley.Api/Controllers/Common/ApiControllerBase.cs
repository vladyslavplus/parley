using Microsoft.AspNetCore.Mvc;
using Parley.Api.Localization;
using Parley.SharedKernel.Results;

namespace Parley.Api.Controllers.Common;

/// <summary>
/// Base for thin controllers mapping results to HTTP responses.
/// </summary>
[ApiController]
public abstract class ApiControllerBase(ErrorTextLocalizer errorTexts) : ControllerBase
{
    protected IActionResult FromResult(Result result) =>
        result.IsSuccess ? NoContent() : Problem(result.Error!);

    protected IActionResult FromResult<TValue>(Result<TValue> result, Func<TValue, IActionResult> mapSuccess) =>
        result.IsSuccess ? mapSuccess(result.Value!) : Problem(result.Error!);

    protected ObjectResult Problem(Error error)
    {
        var detail = errorTexts.Get(error);
        var problemDetails = new ProblemDetails
        {
            Status = MapStatus(error.Type),
            Title = detail,
            Detail = detail,
            Extensions =
            {
                [ApiConstants.ProblemCode] = error.Code,
                [ApiConstants.ProblemTraceId] = HttpContext.TraceIdentifier,
            },
        };

        if (error.FieldErrors is { Count: > 0 })
        {
            problemDetails.Extensions[ApiConstants.ProblemErrors] = error.FieldErrors
                .GroupBy(fieldError => fieldError.Field, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key, object (group) => group.Select(object (fieldError) => new Dictionary<string, object?>
                    {
                        [ApiConstants.ProblemFieldCode] = fieldError.Code,
                        [ApiConstants.ProblemFieldMessage] = errorTexts.Get(fieldError),
                        [ApiConstants.ProblemFieldParameters] = fieldError.Parameters is null
                            ? new Dictionary<string, string>()
                            : fieldError.Parameters.ToDictionary(pair => pair.Key, pair => pair.Value),
                    }).ToList());
        }

        return StatusCode(problemDetails.Status!.Value, problemDetails);
    }

    private static int MapStatus(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status500InternalServerError,
    };
}
