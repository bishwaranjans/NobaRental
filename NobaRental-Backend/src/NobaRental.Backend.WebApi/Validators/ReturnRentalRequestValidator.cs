using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;

namespace NobaRental.Backend.WebApi.Validators;

public class ReturnRentalRequestValidator : AbstractValidator<ReturnRentalRequest>
{
    public ReturnRentalRequestValidator()
    {
        RuleFor(x => x.ReturnStationCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.ReturnMeterReadingKm)
            .GreaterThanOrEqualTo(0);
    }
}
