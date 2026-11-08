using System.Globalization;
using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parley.Api;
using Parley.Api.ErrorHandling;
using Parley.Api.Localization;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Persistence.Entities;
using Parley.SharedKernel.Tenancy.Exceptions;
using Parley.SharedKernel.Tenancy.Implementations;
using Xunit;

namespace Parley.IntegrationTests.Tenancy;

/// <summary>
/// Proves tenant isolation, fail-closed tenant access, timestamp stamping and
/// the stale-write 409 mapping against a real Postgres database.
/// </summary>
public sealed class TenancyTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = fixture;

    public async ValueTask InitializeAsync() =>
        await _fixture.ResetDataAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FreshQuery_OtherOrganizationRow_ReturnsNothing()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationA = Guid.CreateVersion7();
        var organizationB = Guid.CreateVersion7();

        await using (var contextB = ContextFor(organizationB))
        {
            contextB.TenantRecords.Add(TenantRecordFakes.For(organizationB).Generate());
            await contextB.SaveChangesAsync(token);
        }

        await using (var contextA = ContextFor(organizationA))
        {
            var rows = await contextA.TenantRecords.ToListAsync(token);
            rows.Should().BeEmpty("the global tenant filter hides every other organization row");
        }

        await using (var readB = ContextFor(organizationB))
        {
            var ownRows = await readB.TenantRecords.ToListAsync(token);
            ownRows.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task UnsetTenantContext_Query_ThrowsTenantContextNotSetException()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = UnsetTenantContext();

        Exception? captured = null;
        try
        {
            await context.TenantRecords.ToListAsync(token);
        }
        catch (InvalidOperationException exception)
        {
            captured = exception;
        }

        captured.Should().NotBeNull("an unset tenant fails closed instead of returning all rows");

        Exception current = captured!;
        var messages = new List<string> { current.Message };
        while (current.InnerException is not null)
        {
            current = current.InnerException;
            messages.Add(current.Message);
        }

        messages.Should().ContainSingle(message => message.Contains("tenant", StringComparison.OrdinalIgnoreCase),
            "the query failure carries the fail-closed tenant exception message as its cause");
    }

    [Fact]
    public async Task UnsetTenantContext_Write_ThrowsTenantContextNotSetException()
    {
        var token = TestContext.Current.CancellationToken;
        await using var context = UnsetTenantContext();
        context.TenantRecords.Add(new TenantRecord(Guid.CreateVersion7(), "must not persist"));

        var save = async () => await context.SaveChangesAsync(token);
        await save.Should().ThrowAsync<TenantContextNotSetException>(
            "an unset tenant fails closed instead of writing an empty organization");
    }

    [Fact]
    public async Task TimestampStamping_UsesFakeTimeProvider()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        await using var context = ContextFor(organizationId);

        var record = TenantRecordFakes.For(organizationId).Generate();
        context.TenantRecords.Add(record);
        await context.SaveChangesAsync(token);

        var inserted = _fixture.TimeProvider.GetUtcNow().UtcDateTime;
        record.CreatedAt.Should().Be(inserted);
        record.UpdatedAt.Should().Be(inserted);
        record.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);

        _fixture.TimeProvider.Advance(TimeSpan.FromHours(1));
        record.Name = "updated name";
        await context.SaveChangesAsync(token);

        record.CreatedAt.Should().Be(inserted);
        record.UpdatedAt.Should().Be(inserted.AddHours(1));
    }

    [Fact]
    public async Task StaleUpdate_ThrowsDbUpdateConcurrencyException()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();

        await using (var seed = ContextFor(organizationId))
        {
            seed.TenantRecords.Add(TenantRecordFakes.For(organizationId).Generate());
            await seed.SaveChangesAsync(token);
        }

        await using var first = ContextFor(organizationId);
        await using var second = ContextFor(organizationId);

        var firstRow = await first.TenantRecords.FirstAsync(token);
        var secondRow = await second.TenantRecords.FirstAsync(token);

        firstRow.Name = "first writer";
        await first.SaveChangesAsync(token);

        secondRow.Name = "second writer";
        var save = async () => await second.SaveChangesAsync(token);
        await save.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "the stale writer must not overwrites silently");
    }

    [Fact]
    public async Task Model_CompositeIndexStartsWithOrganizationId()
    {
        var organizationId = Guid.CreateVersion7();
        var tenantContext = new JobTenantContext();
        tenantContext.Set(organizationId);
        var options = TenantTestDbContextFactory.CreateOptions(
            _fixture.ConnectionString, tenantContext, _fixture.TimeProvider);

        await using var context = new TenantTestDbContext(options, tenantContext);
        var entityType = context.Model.FindEntityType(typeof(TenantRecord));
        entityType.Should().NotBeNull();

        var indexes = entityType!.GetIndexes().ToArray();
        indexes.Should().HaveCount(1, "the tenant-record table carries exactly one composite index");
        indexes[0].Properties[0].Name.Should().Be(nameof(BusinessEntity.OrganizationId),
            "composite indexes always begin with organization_id");
        indexes[0].Properties.Select(property => property.Name).Should().ContainInOrder(
            nameof(BusinessEntity.OrganizationId), nameof(TenantRecord.Name));
    }

    [Fact]
    public async Task ExplicitIgnoreQueryFilters_ReturnsOtherOrganizationRows()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationA = Guid.CreateVersion7();
        var organizationB = Guid.CreateVersion7();

        await using (var contextB = ContextFor(organizationB))
        {
            contextB.TenantRecords.Add(TenantRecordFakes.For(organizationB).Generate());
            await contextB.SaveChangesAsync(token);
        }

        await using var contextA = ContextFor(organizationA);

        // This is the single explicit escape hatch, used only here to prove the
        // filter is what enforces isolation. The call name makes the bypass
        // visible; production code never calls IgnoreQueryFilters.
        var rows = await contextA.TenantRecords.IgnoreQueryFilters().ToListAsync(token);
        rows.Should().HaveCount(1, "an explicit bypass is the only way past the filter");
    }

    [Theory]
    [InlineData(CultureCodes.English)]
    [InlineData(CultureCodes.Ukrainian)]
    public async Task ConcurrencyConflict_MapsTo409ProblemDetails(string language)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(language);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

        try
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var errorTexts = scope.ServiceProvider.GetRequiredService<ErrorTextLocalizer>();
            var problemDetailsService = scope.ServiceProvider.GetRequiredService<IProblemDetailsService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger, errorTexts, problemDetailsService);

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            httpContext.Response.StatusCode = 200;

            var handled = await handler.TryHandleAsync(
                httpContext,
                new DbUpdateConcurrencyException("stale xmin", new InvalidOperationException("row changed")),
                TestContext.Current.CancellationToken);

            handled.Should().BeTrue();
            httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            httpContext.Response.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(httpContext.Response.Body, default, TestContext.Current.CancellationToken);
            document.RootElement.GetProperty(ApiConstants.ProblemCode).GetString().Should().Be(ErrorCodes.ConcurrencyConflict);
            document.RootElement.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
            document.RootElement.GetProperty(ApiConstants.ProblemTraceId).GetString().Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private TenantTestDbContext ContextFor(Guid organizationId)
    {
        var tenantContext = new JobTenantContext();
        tenantContext.Set(organizationId);
        return TenantTestDbContextFactory.Create(_fixture.ConnectionString, tenantContext, _fixture.TimeProvider);
    }

    private TenantTestDbContext UnsetTenantContext() =>
        TenantTestDbContextFactory.Create(_fixture.ConnectionString, new JobTenantContext(), _fixture.TimeProvider);

}
