using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Cars;

public partial class AddCarDialog(
    ICarApiClient carApiClient,
    ICarCategoryApiClient categoryApiClient,
    IStationApiClient stationApiClient,
    ISnackbar snackbar) : ComponentBase
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = null!;
    protected MudForm? Form;
    protected bool IsValid;
    protected bool IsSubmitting;
    protected string RegistrationNumber { get; set; } = string.Empty;
    protected string CategoryCode { get; set; } = string.Empty;
    protected string StationCode { get; set; } = string.Empty;
    protected long InitialMeterReadingKm { get; set; }
    protected decimal BaseDayRental { get; set; } = 500m;
    protected decimal BaseKmPrice { get; set; }
    protected List<CarCategoryResponse> Categories { get; set; } = [];
    protected List<StationResponse> Stations { get; set; } = [];
    protected bool IsSmallCar =>
        Categories.FirstOrDefault(
            x => string.Equals(x.Code, CategoryCode, StringComparison.OrdinalIgnoreCase))
        ?.ChargesKilometers is false;

    protected void OnCategoryChanged(string code)
    {
        CategoryCode = code;
        if (IsSmallCar) BaseKmPrice = 0m;
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            using var categories = await categoryApiClient.GetCategories();
            if (categories.ResponseMessage.IsSuccessStatusCode && categories.GetContent() is { } categoryList)
            {
                Categories = categoryList.ToList();
                CategoryCode = Categories.FirstOrDefault()?.Code ?? string.Empty;
            }
            Stations = (await stationApiClient.GetAllStationsAsync()).ToList();
            StationCode = Stations.FirstOrDefault()?.Code ?? string.Empty;
        }
        catch (Exception ex)
        {
            snackbar.AddError(
                $"Could not load registration data: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
    }

    protected void Cancel() => MudDialog.Cancel();

    protected async Task Submit()
    {
        if (Form is not null) await Form.ValidateAsync();
        if (!IsValid || string.IsNullOrWhiteSpace(CategoryCode)) return;
        IsSubmitting = true;
        try
        {
            var request = new RegisterCarRequest(
                RegistrationNumber.Trim().ToUpperInvariant(),
                CategoryCode,
                InitialMeterReadingKm,
                StationCode,
                BaseDayRental,
                IsSmallCar ? 0m : BaseKmPrice);
            using var response = await carApiClient.RegisterCar(request);
            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            { snackbar.AddSuccess($"Vehicle '{content.RegistrationNumber}' added to fleet!"); MudDialog.Close(DialogResult.Ok(content)); }
            else snackbar.AddError(ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to register vehicle."));
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
