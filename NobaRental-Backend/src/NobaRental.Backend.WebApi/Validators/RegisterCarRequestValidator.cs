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

        RuleFor(x => x.BaseDayRental)
            .GreaterThan(0)
            .WithMessage("Base day rental must be greater than 0.");

        RuleFor(x => x.BaseKmPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Base km price cannot be negative.");

        RuleFor(x => x.CategoryCode)
            .NotEmpty()
            .MaximumLength(20);
    }
}
