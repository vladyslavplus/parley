using FluentValidation;
using Parley.Crm.Features.Deals.Shared;

namespace Parley.Crm.Features.Deals.Create;

public sealed class CreateDealCommandValidator : AbstractValidator<CreateDealCommand>
{
    public CreateDealCommandValidator()
    {
        DealRequestRules.ApplyTo(this, command => command.Title, command => command.Amount, command => command.Currency);
    }
}
