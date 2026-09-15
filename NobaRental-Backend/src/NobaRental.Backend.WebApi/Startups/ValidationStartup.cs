using FluentValidation;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Validators;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace NobaRental.Backend.WebApi.Startups;

internal static class ValidationStartup
{
    public static IServiceCollection ConfigureValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterPickupRequest>, RegisterPickupRequestValidator>();
        services.AddScoped<IValidator<ReturnRentalRequest>, ReturnRentalRequestValidator>();
        services.AddScoped<IValidator<RegisterCarRequest>, RegisterCarRequestValidator>();
        services.AddScoped<IValidator<UpdateCarTariffRequest>, UpdateCarTariffRequestValidator>();
        services.AddScoped<IValidator<CreateStationRequest>, CreateStationRequestValidator>();
        services.AddScoped<IValidator<UpdateStationRequest>, UpdateStationRequestValidator>();
        services.AddScoped<IValidator<CreateCarCategoryRequest>, CreateCarCategoryRequestValidator>();
        services.AddScoped<IValidator<UpdateCarCategoryRequest>, UpdateCarCategoryRequestValidator>();
        services.AddFluentValidationAutoValidation();

        return services;
    }
}
