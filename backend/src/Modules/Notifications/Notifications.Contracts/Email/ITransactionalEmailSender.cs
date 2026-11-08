using Parley.SharedKernel.Results;

namespace Parley.Notifications.Contracts.Email;

/// <summary>
/// Sends transactional email through SMTP using a stored template.
/// </summary>
/// <remarks>
/// The sender performs external I/O and must never be called inside a
/// database transaction. Callers send after the transaction commits, or
/// enqueue the send through the transactional outbox.
/// <para>
/// Culture is always explicit: the caller passes the recipient culture and
/// the sender never reads ambient culture. Supported cultures are
/// <c>uk</c> and <c>en</c>. Any other value, including empty, falls back to
/// the configured <c>Email:DefaultCulture</c>. A regional form such as
/// <c>uk-UA</c> maps to its language.
/// </para>
/// <para>
/// Delivery failures are returned as a failed <see cref="Result"/>; the
/// method does not throw for delivery problems.
/// </para>
/// </remarks>
public interface ITransactionalEmailSender
{
    /// <summary>
    /// Sends the template identified by <paramref name="templateKey"/> to
    /// <paramref name="recipientAddress"/> in the given explicit
    /// <paramref name="culture"/>.
    /// </summary>
    /// <param name="recipientAddress">Recipient email address.</param>
    /// <param name="templateKey">One of the <see cref="EmailTemplateKey"/> instances.</param>
    /// <param name="culture">Explicit recipient culture, for example <c>uk</c> or <c>en</c>.</param>
    /// <param name="variables">Template values keyed by <see cref="EmailVariableNames"/> constants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> SendAsync(
        string recipientAddress,
        EmailTemplateKey templateKey,
        string culture,
        EmailTemplateVariables? variables = null,
        CancellationToken cancellationToken = default);
}
