using Microsoft.AspNetCore.Mvc;
using Parley.IntegrationTests.Infrastructure.Hosting;

namespace Parley.IntegrationTests.Api.Support;

/// <summary>Throws an unhandled exception to exercise the unexpected-error ProblemDetails path.</summary>
[ApiController]
[Route(TestOnlyRoutes.Throw)]
public sealed class ThrowingTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => throw new InvalidOperationException(TestOnlyRoutes.ThrowMessage);
}
