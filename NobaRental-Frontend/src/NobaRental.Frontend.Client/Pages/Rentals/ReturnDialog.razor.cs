using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
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
            snackbar.AddError($"Could not load stations: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }

        await UpdateEstimateAsync();
    }

    protected int? EstimatedDays { get; set; }
    protected long? EstimatedKm { get; set; }
    protected decimal? EstimatedTotalPrice { get; set; }
    protected string? EstimateCurrency { get; set; }
    protected bool IsEstimating { get; set; }

    private DateTimeOffset GetReturnDateTime()
    {
        var now = timeProvider.GetLocalNow();
        var date = ReturnDate ?? now.Date;
        var time = ReturnTime ?? now.TimeOfDay;
        return new DateTimeOffset(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, TimeSpan.Zero);
    }

    protected async Task UpdateEstimateAsync()
    {
        if (ReturnMeterReadingKm < Booking.PickupMeterReadingKm)
        {
            EstimatedDays = null;
            EstimatedKm = null;
            EstimatedTotalPrice = null;
            return;
        }

        try
        {
            IsEstimating = true;
            var request = new EstimatePriceRequest(
                Booking.BookingNumber,
                GetReturnDateTime(),
                ReturnMeterReadingKm);

            using var response = await apiClient.EstimatePrice(request);
            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            {
                EstimatedDays = content.CalculatedDays;
                EstimatedKm = content.CalculatedKm;
                EstimatedTotalPrice = content.EstimatedPrice;
                EstimateCurrency = content.Currency;
            }
            else
            {
                EstimatedDays = null;
                EstimatedKm = null;
                EstimatedTotalPrice = null;
            }
        }
        catch
        {
            EstimatedDays = null;
            EstimatedKm = null;
            EstimatedTotalPrice = null;
        }
        finally
        {
            IsEstimating = false;
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
            var ifMatch = Booking.RowVersion is { Length: > 0 }
                ? $"\"{Convert.ToBase64String(Booking.RowVersion)}\""
                : null;

            var request = new ReturnRentalRequest(
                ReturnStationCode: ReturnStationCode,
                ReturnDateTime: GetReturnDateTime(),
                ReturnMeterReadingKm: ReturnMeterReadingKm,
                RowVersion: Booking.RowVersion);

            using var response = await apiClient.ReturnBooking(Booking.BookingNumber, request, ifMatch);

            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            {
                snackbar.AddSuccess($"Return registered! Total price: {content.TotalPrice:N2} NOK");
                MudDialog.Close(DialogResult.Ok(content));
            }
            else if (response.ResponseMessage.StatusCode is System.Net.HttpStatusCode.PreconditionFailed or System.Net.HttpStatusCode.Conflict)
            {
                snackbar.AddError("This rental booking was modified by another operation or has already been returned. Please refresh.");
            }
            else
            {
                var error = ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to register return.");
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
