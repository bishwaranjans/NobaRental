using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;

namespace NobaRental.Backend.WebApi.Validators;

public class UpdateCarTariffRequestValidator : AbstractValidator<UpdateCarTariffRequest>
{
    public UpdateCarTariffRequestValidator()
    {
        RuleFor(x => x.BaseDayRental)
            .GreaterThan(0);

        RuleFor(x => x.BaseKmPrice)
            .GreaterThanOrEqualTo(0);
    }
}
