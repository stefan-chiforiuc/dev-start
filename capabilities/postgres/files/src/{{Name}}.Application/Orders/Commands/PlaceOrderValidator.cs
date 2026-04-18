using FluentValidation;

namespace {{Name}}.Application.Orders.Commands;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrder>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(li => li.Sku).NotEmpty();
            l.RuleFor(li => li.Quantity).GreaterThan(0);
            l.RuleFor(li => li.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
