using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;

namespace NobaRental.Backend.WebApi.Validators;

public class RegisterReturnRequestValidator : AbstractValidator<RegisterReturnRequest>
{
    public RegisterReturnRequestValidator()
    {
        RuleFor(x => x.BookingNumber)
            .GreaterThan(0);

        RuleFor(x => x.ReturnStationCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.ReturnMeterReadingKm)
            .GreaterThanOrEqualTo(0);
    }
}
