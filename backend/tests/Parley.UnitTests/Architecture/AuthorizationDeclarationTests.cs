using System.Reflection;
using AwesomeAssertions;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Parley.Api.Controllers.Auth;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;
using Xunit;

namespace Parley.UnitTests.Architecture;

/// <summary>
/// Every Mediator request in the production assemblies declares either a permission or
/// one explicit opt-out, and every HTTP action declares the same, so a forgotten
/// declaration fails the test suite.
/// </summary>
public sealed class AuthorizationDeclarationTests
{
    private static readonly string[] ProductionAssemblyNames =
    [
        "Parley.SharedKernel",
        "Parley.Identity",
        "Parley.Crm",
        "Parley.Campaigns",
        "Parley.Insights",
        "Parley.Notifications",
    ];

    [Fact]
    public void Requests_DeclarePermissionOrOptOut()
    {
        var requests = ProductionAssemblyNames
            .Select(Assembly.Load)
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IMessage).IsAssignableFrom(type))
            .ToArray();

        requests.Should().NotBeEmpty("the identity module defines requests");

        var undeclared = requests
            .Where(type => !DeclaresExactlyOne(type))
            .Select(type => type.FullName)
            .ToArray();
        undeclared.Should().BeEmpty("every Mediator request needs RequiresPermission or PermissionOptOut");
    }

    /// <summary>
    /// The authorization pipeline behavior is constrained to responses that implement
    /// <c>IResult&lt;TSelf&gt;</c>. A request with another response type could fall outside that
    /// constraint, so this test keeps every request response on the Result abstraction.
    /// </summary>
    [Fact]
    public void Requests_ResponseImplementsResultAbstraction()
    {
        var responseTypes = ProductionAssemblyNames
            .Select(Assembly.Load)
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IRequest<>))
            .Select(implemented => implemented.GenericTypeArguments[0])
            .Distinct()
            .ToArray();

        responseTypes.Should().NotBeEmpty("the identity module defines requests");

        var outsideAbstraction = responseTypes
            .Where(response => !typeof(IResult<>).MakeGenericType(response).IsAssignableFrom(response))
            .Select(response => response.FullName)
            .ToArray();
        outsideAbstraction.Should().BeEmpty("every request response must implement IResult<TSelf> so the authorization behavior applies");
    }

    [Fact]
    public void ControllerActions_DeclarePermissionOrOptOut()
    {
        var controllers = typeof(AuthController).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();
        controllers.Should().NotBeEmpty("the API defines controllers");

        var actions = controllers
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true).Length > 0)
            .ToArray();
        actions.Should().NotBeEmpty();

        var undeclared = actions
            .Where(method => !DeclaresExactlyOne(method))
            .Select(method => $"{method.DeclaringType?.FullName}.{method.Name}")
            .ToArray();
        undeclared.Should().BeEmpty("every endpoint needs RequiresPermission or PermissionOptOut on the action");
    }

    private static bool DeclaresExactlyOne(ICustomAttributeProvider declaration)
    {
        var permissions = declaration.GetCustomAttributes(typeof(RequiresPermissionAttribute), inherit: false).Length;
        var optOuts = declaration.GetCustomAttributes(typeof(PermissionOptOutAttribute), inherit: false).Length;
        return permissions + optOuts == 1;
    }
}
