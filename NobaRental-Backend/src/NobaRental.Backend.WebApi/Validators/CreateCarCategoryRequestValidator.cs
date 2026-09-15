using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;

namespace NobaRental.Backend.WebApi.Validators;

public class CreateCarCategoryRequestValidator : AbstractValidator<CreateCarCategoryRequest>
{
    public CreateCarCategoryRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DayMultiplier)
            .GreaterThan(0);

        RuleFor(x => x.KmMultiplier)
            .GreaterThanOrEqualTo(0);
    }
}
