using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Cars;

public partial class AddCarDialog(
    ICarApiClient carApiClient,
    IStationApiClient stationApiClient,
    ISnackbar snackbar) : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    protected MudForm? Form;
    protected bool IsValid;
    protected bool IsSubmitting;

    protected string RegistrationNumber { get; set; } = string.Empty;
    protected CarCategoryDto Category { get; set; } = CarCategoryDto.SmallCar;
    protected string StationCode { get; set; } = string.Empty;
    protected long InitialMeterReadingKm { get; set; }
    protected decimal BaseDayRental { get; set; } = 500m;
    protected decimal BaseKmPrice { get; set; } = 0m;

    protected bool IsSmallCar => Category == CarCategoryDto.SmallCar;

    protected void OnCategoryChanged(CarCategoryDto newCategory)
    {
        Category = newCategory;
        if (newCategory == CarCategoryDto.SmallCar)
        {
            BaseKmPrice = 0m;
        }
    }

    protected List<StationResponse> Stations = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var list = await stationApiClient.GetAllStationsAsync();
            Stations = list.ToList();
            if (Stations.Count > 0)
            {
                StationCode = Stations[0].Code;
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Could not load stations: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
    }

    protected void Cancel() => MudDialog.Cancel();

    protected async Task Submit()
    {
        if (Form is not null)
        {
            await Form.ValidateAsync();
        }

        if (!IsValid)
        {
            return;
        }

        IsSubmitting = true;

        try
        {
            var request = new RegisterCarRequest(
                RegistrationNumber: RegistrationNumber.Trim().ToUpperInvariant(),
                Category: Category,
                InitialMeterReadingKm: InitialMeterReadingKm,
                StationCode: StationCode,
                BaseDayRental: BaseDayRental,
                BaseKmPrice: IsSmallCar ? 0m : BaseKmPrice);

            using var response = await carApiClient.RegisterCar(request);

            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            {
                snackbar.AddSuccess($"Vehicle '{content.RegistrationNumber}' added to fleet!");
                MudDialog.Close(DialogResult.Ok(content));
            }
            else
            {
                var error = ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to register vehicle.");
                snackbar.AddError(error);
            }
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
