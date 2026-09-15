using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;

using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Cars;

public partial class EditTariffDialog(
    ICarApiClient carApiClient,
    ISnackbar snackbar) : ComponentBase
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter, EditorRequired]
    public required CarResponse Car { get; set; }

    protected MudForm? Form;
    protected bool IsValid;
    protected bool IsSubmitting;

    protected decimal BaseDayRental { get; set; }
    protected decimal BaseKmPrice { get; set; }

    protected static bool IsSmallCar => false;

    protected override void OnInitialized()
    {
        BaseDayRental = Car.BaseDayRental;
        BaseKmPrice = IsSmallCar ? 0m : Car.BaseKmPrice;
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
            var ifMatch = Car.RowVersion is { Length: > 0 }
                ? $"\"{Convert.ToBase64String(Car.RowVersion)}\""
                : null;

            var request = new UpdateCarTariffRequest(
                BaseDayRental: BaseDayRental,
                BaseKmPrice: IsSmallCar ? 0m : BaseKmPrice,
                RowVersion: Car.RowVersion);

            using var response = await carApiClient.UpdateCarTariff(Car.RegistrationNumber, request, ifMatch);

            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            {
                snackbar.AddSuccess($"Tariff updated for vehicle '{content.RegistrationNumber}'.");
                MudDialog.Close(DialogResult.Ok(content));
            }
            else if (response.ResponseMessage.StatusCode is System.Net.HttpStatusCode.PreconditionFailed or System.Net.HttpStatusCode.Conflict)
            {
                snackbar.AddError("The vehicle details were modified by another operation. Please refresh and try again.");
            }
            else
            {
                var error = ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to update vehicle tariff.");
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


