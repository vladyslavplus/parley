using FluentValidation;
using Parley.Crm.Features.Tasks.Shared;

namespace Parley.Crm.Features.Tasks.Update;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        TaskRequestRules.ApplyTo(this, command => command.Title, command => command.Notes);
    }
}
