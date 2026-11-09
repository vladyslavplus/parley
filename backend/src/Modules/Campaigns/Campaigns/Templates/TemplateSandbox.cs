using System.Globalization;
using System.Text.Encodings.Web;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates.Model;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Templates;

/// <summary>
/// Parses and renders the Liquid templates of the campaigns module in a sandbox. Validation runs in three steps:
/// the length limit, the lexical check of <see cref="TemplateScanner"/> (tags and variables), then Fluid's parser,
/// which runs with functions and parentheses disabled. Rendering reads only <see cref="TemplateModel"/> through explicit
/// member registrations, has no file provider (so no <c>include</c> or <c>render</c> reads), strict variables and filters,
/// and bounded steps, recursion and output.
/// </summary>
public sealed class TemplateSandbox
{
    private readonly FluidParser _parser = new(new FluidParserOptions
    {
        AllowFunctions = false,
        AllowParentheses = false,
    });

    private readonly TemplateOptions _options = CreateOptions();

    /// <summary>Checks a template source and parses it. The returned template is safe to render with <see cref="RenderAsync"/>.</summary>
    /// <param name="source">Liquid source as the user wrote it.</param>
    /// <param name="maximumLength">Longest source accepted, in characters.</param>
    public Result<IFluidTemplate> Parse(string source, int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Length > maximumLength)
        {
            return Result<IFluidTemplate>.Failure(Error.Validation(CampaignsErrorCodes.TemplateTooLong));
        }

        var finding = TemplateScanner.FindFirst(source);
        if (finding is not null)
        {
            var position = TemplatePosition.From(source, finding.Offset);
            return Result<IFluidTemplate>.Failure(Error.Validation(finding.Code, NamedPosition(finding.Name, position)));
        }

        try
        {
            return Result<IFluidTemplate>.Success(_parser.Parse(source));
        }
        catch (ParseException)
        {
            var position = TemplateSyntaxLocator.Locate(source, _parser) ?? new TemplatePosition(1, 1);
            return Result<IFluidTemplate>.Failure(Error.Validation(CampaignsErrorCodes.TemplateSyntaxInvalid, Position(position)));
        }
    }

    /// <summary>
    /// Renders a parsed template. Values are written through <paramref name="encoder"/>: pass the HTML encoder for body
    /// text and the null encoder for subjects. The output goes to a writer that stops at the output limit, so a render
    /// never builds more text than that. Failures inside the sandbox come back as a validation error with a code.
    /// Fluid 2.41 has no render cancellation hook, so the work is bounded by the step and recursion limits as well.
    /// Subjects are not header-safe: the send path must strip CR and LF before a subject becomes a mail header.
    /// </summary>
    public async ValueTask<Result<string>> RenderAsync(
        IFluidTemplate template,
        TemplateModel model,
        TextEncoder encoder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(encoder);
        cancellationToken.ThrowIfCancellationRequested();

        var context = new TemplateContext(model, _options, false);
        var writer = new TemplateOutputWriter(CampaignsLimits.TemplateMaximumOutputSize);
        try
        {
            await template.RenderAsync(writer, context, encoder, false);
            return Result<string>.Success(writer.ToString());
        }
        catch (Exception exception) when (exception is FluidException or TemplateOutputLimitException)
        {
            return Result<string>.Failure(Error.Validation(CampaignsErrorCodes.TemplateRenderFailed));
        }
    }

    private static TemplateOptions CreateOptions()
    {
        var options = new TemplateOptions
        {
            StrictVariables = true,
            StrictFilters = true,
            FileProvider = new NullFileProvider(),
            CultureInfo = CultureInfo.InvariantCulture,
            MaxRecursion = CampaignsLimits.TemplateMaximumRecursion,
            MaxSteps = CampaignsLimits.TemplateMaximumSteps,
        };

        options.MemberAccessStrategy.Register<TemplateModel, object?>((model, member, _) => member switch
        {
            TemplateVariables.ContactRoot => model.Contact,
            TemplateVariables.CompanyRoot => model.Company,
            TemplateVariables.UnsubscribeUrlRoot => model.UnsubscribeUrl,
            _ => null,
        });
        options.MemberAccessStrategy.Register<ContactTemplateModel, object?>((contact, member, _) => member switch
        {
            TemplateVariables.FirstNameMember => contact.FirstName,
            TemplateVariables.LastNameMember => contact.LastName,
            _ => null,
        });
        options.MemberAccessStrategy.Register<CompanyTemplateModel, object?>((company, member, _) => member switch
        {
            TemplateVariables.CompanyNameMember => company.Name,
            _ => null,
        });

        return options;
    }

    /// <summary>Parameters in placeholder order: the name first, then the position.</summary>
    private static Dictionary<string, string> NamedPosition(string name, TemplatePosition position) => new(StringComparer.Ordinal)
    {
        [CampaignsErrorParameters.Name] = name,
        [CampaignsErrorParameters.Line] = position.Line.ToString(CultureInfo.InvariantCulture),
        [CampaignsErrorParameters.Column] = position.Column.ToString(CultureInfo.InvariantCulture),
    };

    private static Dictionary<string, string> Position(TemplatePosition position) => new(StringComparer.Ordinal)
    {
        [CampaignsErrorParameters.Line] = position.Line.ToString(CultureInfo.InvariantCulture),
        [CampaignsErrorParameters.Column] = position.Column.ToString(CultureInfo.InvariantCulture),
    };
}
