using Mediator;
using Microsoft.Extensions.Options;
using Parley.Notifications.Features.Telegram.Shared;
using Parley.Notifications.Options;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Telegram.Status;

public sealed class GetTelegramStatusQueryHandler(IOptions<TelegramOptions> options)
    : IRequestHandler<GetTelegramStatusQuery, Result<TelegramStatusView>>
{
    public ValueTask<Result<TelegramStatusView>> Handle(GetTelegramStatusQuery query, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var enabled = TelegramOptionRules.IsConfigured(settings);
        var view = new TelegramStatusView(
            enabled,
            settings.Mode.ToString().ToLowerInvariant(),
            enabled ? settings.BotUsername : null);
        return ValueTask.FromResult(Result<TelegramStatusView>.Success(view));
    }
}
