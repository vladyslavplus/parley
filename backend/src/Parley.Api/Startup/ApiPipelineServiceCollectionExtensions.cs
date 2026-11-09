using Microsoft.AspNetCore.Mvc;
using Parley.Api.ErrorHandling;
using Parley.Api.Localization;
using Parley.Api.Startup.OpenApi;

namespace Parley.Api.Startup;

public static class ApiPipelineServiceCollectionExtensions
{
    /// <summary>
    /// Registers controllers, OpenAPI, localization, ProblemDetails and the exception handler.
    /// </summary>
    public static IServiceCollection AddApiPipeline(this IServiceCollection services)
    {
        services.AddControllers();
        // Model binding failures reach the handlers, which report them as the shared
        // validation ProblemDetails instead of the framework's default shape.
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(ApiConstants.OpenApiDocumentName, options => options
            .AddSchemaTransformer<StringEnumSchemaTransformer>()
            .AddOperationTransformer<ListParameterOperationTransformer>());

        services.AddSingleton(TimeProvider.System);
        services.AddLocalization();
        services.AddSingleton<ErrorTextLocalizer>();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
