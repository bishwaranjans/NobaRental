using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;

namespace NobaRental.Backend.WebApi.Validators;

public class RegisterPickupRequestValidator : AbstractValidator<RegisterPickupRequest>
{
    public RegisterPickupRequestValidator()
    {
        RuleFor(x => x.RegistrationNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.CustomerSsn)
            .Matches(@"^\d{11}$")
            .WithMessage("Customer SSN must contain exactly 11 digits.");

        RuleFor(x => x.PickupStationCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.PickupMeterReadingKm)
            .GreaterThanOrEqualTo(0);
    }
}
