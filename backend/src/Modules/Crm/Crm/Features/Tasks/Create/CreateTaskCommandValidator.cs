using FluentValidation;
using Parley.Crm.Features.Tasks.Shared;

namespace Parley.Crm.Features.Tasks.Create;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        TaskRequestRules.ApplyTo(this, command => command.Title, command => command.Notes);
    }
}
