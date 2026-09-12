using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;

namespace NobaRental.Backend.WebApi.Validators;

public class RegisterCarRequestValidator : AbstractValidator<RegisterCarRequest>
{
    public RegisterCarRequestValidator()
    {
        RuleFor(x => x.RegistrationNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.InitialMeterReadingKm)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.StationCode)
            .NotEmpty()
            .MaximumLength(10);
    }
}
