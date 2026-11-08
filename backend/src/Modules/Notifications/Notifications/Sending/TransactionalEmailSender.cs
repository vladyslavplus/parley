using System.Text.Encodings.Web;
using Fluid;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Parley.Notifications.Contracts.Email;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Options;
using Parley.Notifications.Templates;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Sending;

/// <summary>
/// SMTP implementation of <see cref="ITransactionalEmailSender"/> using
/// MailKit and Fluid templates. Logs only the template key and culture,
/// never the recipient address, subject or body.
/// </summary>
public sealed class TransactionalEmailSender(
    IOptions<EmailOptions> options,
    FluidParser fluidParser,
    TimeProvider timeProvider,
    ILogger<TransactionalEmailSender> logger) : ITransactionalEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task<Result> SendAsync(
        string recipientAddress,
        EmailTemplateKey templateKey,
        string culture,
        EmailTemplateVariables? variables = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateKey);

        var resolvedCulture = EmailCultureResolver.Resolve(culture, _options.DefaultCulture);
        logger.LogInformation(
            "Transactional email send attempted. TemplateKey={TemplateKey} Culture={Culture}",
            templateKey.Value,
            resolvedCulture);

        var result = await SendCoreAsync(recipientAddress, templateKey, resolvedCulture, variables, cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning(
                "Transactional email send failed. TemplateKey={TemplateKey} Culture={Culture} ErrorCode={ErrorCode}",
                templateKey.Value,
                resolvedCulture,
                result.Error?.Code);
        }

        return result;
    }

    private async Task<Result> SendCoreAsync(
        string recipientAddress,
        EmailTemplateKey templateKey,
        string culture,
        EmailTemplateVariables? variables,
        CancellationToken cancellationToken)
    {
        if (!EmailAddressRules.IsValid(recipientAddress))
        {
            return Result.Failure(Error.Validation(NotificationErrorCodes.InvalidRecipient));
        }

        var values = variables?.Values ?? EmailTemplateVariables.Empty.Values;
        if (!HasRequiredVariable(templateKey, values))
        {
            return Result.Failure(Error.Validation(NotificationErrorCodes.InvalidTemplateVariables));
        }

        var rendered = await RenderMessageAsync(recipientAddress, templateKey, culture, values, cancellationToken);
        if (!rendered.IsSuccess)
        {
            return Result.Failure(rendered.Error!);
        }

        return await SendViaSmtpAsync(rendered.Value!, cancellationToken);
    }

    private async Task<Result<MimeMessage>> RenderMessageAsync(
        string recipientAddress,
        EmailTemplateKey templateKey,
        string culture,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var subjectSource = EmailTemplateStore.Load(templateKey, culture, EmailTemplateStore.SubjectPart);
        var htmlSource = EmailTemplateStore.Load(templateKey, culture, EmailTemplateStore.HtmlPart);
        var textSource = EmailTemplateStore.Load(templateKey, culture, EmailTemplateStore.TextPart);
        if (subjectSource is null || htmlSource is null || textSource is null)
        {
            return Result<MimeMessage>.Failure(Error.NotFound(NotificationErrorCodes.TemplateNotFound));
        }

        try
        {
            // Subject and plain text are not HTML, so they are rendered without
            // HTML encoding. Only the HTML part encodes user-provided values.
            var subject = await RenderAsync(subjectSource, values, NullEncoder.Default, cancellationToken);
            var html = await RenderAsync(htmlSource, values, HtmlEncoder.Default, cancellationToken);
            var text = await RenderAsync(textSource, values, NullEncoder.Default, cancellationToken);
            return Result<MimeMessage>.Success(BuildMessage(recipientAddress, subject, html, text));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result<MimeMessage>.Failure(Error.Failure(NotificationErrorCodes.TemplateRenderFailed));
        }
    }

    private async Task<string> RenderAsync(
        string source,
        IReadOnlyDictionary<string, string> values,
        TextEncoder encoder,
        CancellationToken cancellationToken)
    {
        if (!fluidParser.TryParse(source, out var template, out var parseError) || template is null)
        {
            throw new InvalidOperationException(parseError);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var context = new TemplateContext();
        foreach (var (key, value) in values)
        {
            context.SetValue(key, value);
        }

        return await template.RenderAsync(context, encoder);
    }

    private MimeMessage BuildMessage(string recipientAddress, string subject, string html, string text)
    {
        var message = new MimeMessage
        {
            Date = timeProvider.GetUtcNow(),
            Subject = subject,
        };
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipientAddress.Trim()));
        message.Body = new BodyBuilder
        {
            HtmlBody = html,
            TextBody = text,
        }.ToMessageBody();
        return message;
    }

    private async Task<Result> SendViaSmtpAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        client.Timeout = (int)_options.Timeout.TotalMilliseconds;
        using var timeout = new CancellationTokenSource(_options.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, _options.SecureSocketOption, linked.Token);
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, linked.Token);
            }

            await client.SendAsync(message, linked.Token);
            await client.DisconnectAsync(true, linked.Token);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure(Error.Failure(NotificationErrorCodes.SmtpFailed));
        }
        finally
        {
            if (client.IsConnected)
            {
                try
                {
                    // Best effort on the failure path; the client Timeout bounds the wait.
                    await client.DisconnectAsync(false, CancellationToken.None);
                }
                catch (Exception)
                {
                    // The connection is already unusable; nothing more to release.
                }
            }
        }
    }

    private static bool HasRequiredVariable(EmailTemplateKey templateKey, IReadOnlyDictionary<string, string> values)
    {
        var variableName = templateKey == EmailTemplateKey.PasswordReset
            ? EmailVariableNames.ResetLink
            : templateKey == EmailTemplateKey.Invitation
                ? EmailVariableNames.InvitationLink
                : EmailVariableNames.ConfirmationLink;
        return values.TryGetValue(variableName, out var value) && !string.IsNullOrWhiteSpace(value);
    }
}
