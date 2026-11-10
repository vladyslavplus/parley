using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Api;

/// <summary>
/// Two different CLR types with the same OpenAPI schema id collapse into one component of the document, so the generated
/// client types one of them wrongly. The schema id is the type name, with generic arguments appended. This walks the types
/// the endpoints describe, and their Parley property types, and fails on any id shared by two types.
/// </summary>
public sealed class OpenApiSchemaIdTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ParleyNamespace = "Parley";

    private const char GenericArityMarker = '`';

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public void DocumentedTypes_HaveOneTypePerSchemaId()
    {
        var provider = _factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var documented = DocumentedTypes(provider);

        var collisions = documented
            .GroupBy(SchemaId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(type => type.FullName))}")
            .ToArray();

        collisions.Should().BeEmpty("each OpenAPI schema id must name exactly one CLR type");
    }

    private static HashSet<Type> DocumentedTypes(IApiDescriptionGroupCollectionProvider provider)
    {
        var pending = new Stack<Type>();
        foreach (var group in provider.ApiDescriptionGroups.Items)
        {
            foreach (var description in group.Items)
            {
                foreach (var response in description.SupportedResponseTypes)
                {
                    if (response.Type is { } responseType)
                    {
                        pending.Push(responseType);
                    }
                }

                foreach (var parameter in description.ParameterDescriptions)
                {
                    pending.Push(parameter.Type);
                }
            }
        }

        var visited = new HashSet<Type>();
        while (pending.Count > 0)
        {
            var type = Unwrap(pending.Pop());
            if (!visited.Add(type))
            {
                continue;
            }

            if (type.IsArray)
            {
                pending.Push(type.GetElementType()!);
            }

            if (type.IsGenericType)
            {
                foreach (var argument in type.GetGenericArguments())
                {
                    pending.Push(argument);
                }
            }

            if (IsParley(type))
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    pending.Push(property.PropertyType);
                }
            }
        }

        return visited.Where(IsParley).ToHashSet();
    }

    private static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static bool IsParley(Type type) =>
        type.Namespace is not null && type.Namespace.StartsWith(ParleyNamespace, StringComparison.Ordinal);

    /// <summary>The default schema id: the type name, generic arguments appended after "Of".</summary>
    private static string SchemaId(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf(GenericArityMarker, StringComparison.Ordinal)];
        return name + "Of" + string.Concat(type.GetGenericArguments().Select(SchemaId));
    }
}
