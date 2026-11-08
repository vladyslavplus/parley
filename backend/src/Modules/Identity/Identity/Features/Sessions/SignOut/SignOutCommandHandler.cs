using Mediator;
using Parley.Identity.Authentication.Refresh;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Sessions.SignOut;

public sealed class SignOutCommandHandler(RefreshTokenService refreshTokens) : IRequestHandler<SignOutCommand, Result>
{
    public async ValueTask<Result> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        await refreshTokens.RevokeSessionAsync(command.RefreshToken, cancellationToken);
        return Result.Success();
    }
}
