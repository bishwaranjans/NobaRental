using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Rentals;

public partial class ReturnDialog(
    IRentalBookingApiClient apiClient,
    IStationApiClient stationApiClient,
    ISnackbar snackbar,
    TimeProvider timeProvider) : ComponentBase
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter, EditorRequired]
    public required RentalBookingResponse Booking { get; set; }

    protected MudForm? Form;
    protected bool IsValid;
    protected bool IsSubmitting;

    protected List<StationResponse> Stations { get; set; } = [];
    protected string ReturnStationCode { get; set; } = string.Empty;

    protected DateTime? ReturnDate;
    protected TimeSpan? ReturnTime;
    protected long ReturnMeterReadingKm;

    protected override async Task OnInitializedAsync()
    {
        var now = timeProvider.GetLocalNow();
        ReturnDate = now.Date;
        ReturnTime = now.TimeOfDay;
        ReturnMeterReadingKm = Booking.PickupMeterReadingKm + 50;
        ReturnStationCode = Booking.PickupStationCode;

        try
        {
            var list = await stationApiClient.GetAllStationsAsync();
            Stations = list.ToList();
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Could not load stations: {ex.Message}");
        }
    }

    protected int EstimatedDays
    {
        get
        {
            var now = timeProvider.GetLocalNow();
            var date = ReturnDate ?? now.Date;
            var time = ReturnTime ?? now.TimeOfDay;
            var returnDateTime = new DateTimeOffset(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, TimeSpan.Zero);
            var elapsed = (returnDateTime - Booking.PickupDateTime).TotalDays;
            return Math.Max(1, (int)Math.Ceiling(elapsed));
        }
    }

    protected long EstimatedKm => Math.Max(0, ReturnMeterReadingKm - Booking.PickupMeterReadingKm);

    protected decimal EstimatedTotalPrice
    {
        get
        {
            var days = EstimatedDays;
            var km = EstimatedKm;

            return Booking.Category switch
            {
                CarCategoryDto.SmallCar => Math.Round(Booking.BaseDayRental * days, 2, MidpointRounding.AwayFromZero),
                CarCategoryDto.Combi => Math.Round(Booking.BaseDayRental * days * 1.3m, 2, MidpointRounding.AwayFromZero)
                    + Math.Round(Booking.BaseKmPrice * km, 2, MidpointRounding.AwayFromZero),
                CarCategoryDto.Truck => Math.Round(Booking.BaseDayRental * days * 1.5m, 2, MidpointRounding.AwayFromZero)
                    + Math.Round(Booking.BaseKmPrice * km * 1.5m, 2, MidpointRounding.AwayFromZero),
                _ => 0m
            };
        }
    }

    protected void Cancel() => MudDialog.Cancel();

    protected async Task Submit()
    {
        if (Form is not null)
            await Form.ValidateAsync();

        if (!IsValid)
            return;

        IsSubmitting = true;

        try
        {
            var now = timeProvider.GetLocalNow();
            var date = ReturnDate ?? now.Date;
            var time = ReturnTime ?? now.TimeOfDay;
            var returnDateTime = new DateTimeOffset(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, TimeSpan.Zero);

            var request = new RegisterReturnRequest(
                BookingNumber: Booking.BookingNumber,
                ReturnStationCode: ReturnStationCode,
                ReturnDateTime: returnDateTime,
                ReturnMeterReadingKm: ReturnMeterReadingKm,
                RowVersion: Booking.RowVersion);

            using var response = await apiClient.RegisterReturn(request);

            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            {
                snackbar.AddSuccess($"Return registered! Total price: {content.TotalPrice:N2} NOK");
                MudDialog.Close(DialogResult.Ok(content));
            }
            else
            {
                var error = response.StringContent ?? "Failed to register return.";
                snackbar.AddError(error);
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
