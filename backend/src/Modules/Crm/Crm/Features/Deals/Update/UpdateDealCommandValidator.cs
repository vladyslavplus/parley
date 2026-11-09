using FluentValidation;
using Parley.Crm.Features.Deals.Shared;

namespace Parley.Crm.Features.Deals.Update;

public sealed class UpdateDealCommandValidator : AbstractValidator<UpdateDealCommand>
{
    public UpdateDealCommandValidator()
    {
        DealRequestRules.ApplyTo(this, command => command.Title, command => command.Amount, command => command.Currency);
    }
}
