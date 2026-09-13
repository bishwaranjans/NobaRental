using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Frontend.Client.Helpers;
using NobaRental.Frontend.Client.Pages.Cars;

namespace NobaRental.Frontend.Client.Pages.Rentals;

public partial class PickupDialog(
    IRentalBookingApiClient apiClient,
    ICarApiClient carApiClient,
    IStationApiClient stationApiClient,
    IDialogService dialogService,
    ISnackbar snackbar,
    TimeProvider timeProvider) : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    protected MudForm? Form;
    protected bool IsValid;
    protected bool IsSubmitting;
    protected bool IsCarSelected;

    protected List<StationResponse> Stations { get; set; } = [];
    protected string SelectedStationCode { get; set; } = string.Empty;
    protected List<CarResponse> AvailableCars { get; set; } = [];

    protected decimal SelectedCarDayRental { get; set; }
    protected decimal SelectedCarKmPrice { get; set; }

    protected DateTime? PickupDate;
    protected TimeSpan? PickupTime;

    protected PickupFormModel Model { get; set; } = new()
    {
        RegistrationNumber = string.Empty,
        CustomerSsn = string.Empty,
        Category = CarCategoryDto.SmallCar,
        PickupMeterReadingKm = 0,
    };

    protected override async Task OnInitializedAsync()
    {
        var now = timeProvider.GetLocalNow();
        PickupDate = now.Date;
        PickupTime = now.TimeOfDay;

        try
        {
            var stations = await stationApiClient.GetAllStationsAsync();
            Stations = stations.ToList();
            if (Stations.Count > 0)
            {
                SelectedStationCode = Stations[0].Code;
                await LoadAvailableCarsForStation(SelectedStationCode);
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error loading stations: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
    }

    protected async Task OnStationChanged(string stationCode)
    {
        SelectedStationCode = stationCode;
        await LoadAvailableCarsForStation(stationCode);
    }

    protected async Task LoadAvailableCarsForStation(string stationCode)
    {
        try
        {
            using var response = await carApiClient.GetAvailableCars(stationCode: stationCode);
            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } list)
            {
                AvailableCars = list.ToList();
                if (AvailableCars.Count > 0)
                {
                    OnCarSelected(AvailableCars[0].RegistrationNumber);
                }
                else
                {
                    Model.RegistrationNumber = string.Empty;
                    IsCarSelected = false;
                }
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error loading available cars: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
    }

    protected void OnCarSelected(string regNumber)
    {
        Model.RegistrationNumber = regNumber;
        var car = AvailableCars.FirstOrDefault(c => string.Equals(c.RegistrationNumber, regNumber, StringComparison.OrdinalIgnoreCase));
        if (car is not null)
        {
            Model.Category = car.Category;
            Model.PickupMeterReadingKm = car.CurrentMeterReadingKm;
            SelectedCarDayRental = car.BaseDayRental;
            SelectedCarKmPrice = car.BaseKmPrice;
            IsCarSelected = true;
        }
        else
        {
            SelectedCarDayRental = 0m;
            SelectedCarKmPrice = 0m;
            IsCarSelected = false;
        }
    }

    protected async Task OpenAddCarDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await dialogService.ShowAsync<AddCarDialog>("Register Vehicle", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: CarResponse newCar })
        {
            await LoadAvailableCarsForStation(SelectedStationCode);
            if (string.Equals(newCar.CurrentStationCode, SelectedStationCode, StringComparison.OrdinalIgnoreCase))
            {
                OnCarSelected(newCar.RegistrationNumber);
            }
        }
    }

    protected static string GetCategoryName(CarCategoryDto category) => category switch
    {
        CarCategoryDto.SmallCar => "Small car",
        CarCategoryDto.Combi => "Combi",
        CarCategoryDto.Truck => "Truck",
        _ => category.ToString()
    };

    protected void Cancel() => MudDialog.Cancel();

    protected async Task Submit()
    {
        if (Form is not null)
        {
            await Form.ValidateAsync();
        }

        if (!IsValid || string.IsNullOrWhiteSpace(Model.RegistrationNumber))
        {
            return;
        }

        IsSubmitting = true;

        try
        {
            var now = timeProvider.GetLocalNow();
            var date = PickupDate ?? now.Date;
            var time = PickupTime ?? now.TimeOfDay;
            var pickupDateTime = new DateTimeOffset(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, TimeSpan.Zero);

            var request = new RegisterPickupRequest(
                RegistrationNumber: Model.RegistrationNumber.Trim(),
                CustomerSsn: Model.CustomerSsn.Trim(),
                Category: Model.Category,
                PickupStationCode: SelectedStationCode,
                PickupDateTime: pickupDateTime,
                PickupMeterReadingKm: Model.PickupMeterReadingKm,
                Currency: "NOK");

            using var response = await apiClient.RegisterPickup(request);

            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            {
                snackbar.AddSuccess($"Pickup registered successfully at {SelectedStationCode}! Booking: #{content.BookingNumber}");
                MudDialog.Close(DialogResult.Ok(content));
            }
            else
            {
                var error = ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to register pickup.");
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

    public class PickupFormModel
    {
        public required string RegistrationNumber { get; set; }

        public required string CustomerSsn { get; set; }

        public CarCategoryDto Category { get; set; }

        public long PickupMeterReadingKm { get; set; }
    }
}
