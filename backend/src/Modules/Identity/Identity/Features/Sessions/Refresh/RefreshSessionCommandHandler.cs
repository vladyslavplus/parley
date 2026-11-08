using Mediator;
using Parley.Identity.Authentication.Refresh;
using Parley.Identity.Authentication.Tokens;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Sessions.Refresh;

public sealed class RefreshSessionCommandHandler(RefreshTokenService refreshTokens)
    : IRequestHandler<RefreshSessionCommand, Result<AuthTokens>>
{
    public ValueTask<Result<AuthTokens>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken) =>
        new(refreshTokens.RotateAsync(command.RefreshToken, cancellationToken));
}
