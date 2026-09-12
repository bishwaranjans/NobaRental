using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;

namespace NobaRental.Backend.WebApi.Validators;

public class UpdateStationRequestValidator : AbstractValidator<UpdateStationRequest>
{
    public UpdateStationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(50);
    }
}
