using Mediator;
using Parley.SharedKernel.Results;

namespace Parley.UnitTests.SharedKernel.Mediator.Behaviors.Support;

public sealed record SampleQuery(string? Value) : IRequest<Result<string>>;
