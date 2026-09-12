using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Stations;

public partial class StationDialog(
    IStationApiClient stationApiClient,
    ISnackbar snackbar) : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public StationResponse? ExistingStation { get; set; }

    protected bool IsEditMode => ExistingStation is not null;
    protected MudForm Form = null!;
    protected bool IsValid;
    protected bool IsSubmitting;
    protected StationFormModel Model = new();

    protected override void OnInitialized()
    {
        if (ExistingStation is not null)
        {
            Model = new StationFormModel
            {
                Code = ExistingStation.Code,
                Name = ExistingStation.Name,
                City = ExistingStation.City,
                IsActive = ExistingStation.IsActive
            };
        }
    }

    protected void Cancel() => MudDialog.Cancel();

    protected async Task Submit()
    {
        await Form.ValidateAsync();
        if (!Form.IsValid)
        {
            return;
        }

        IsSubmitting = true;
        try
        {
            if (IsEditMode)
            {
                var request = new UpdateStationRequest(
                    Name: Model.Name,
                    City: Model.City,
                    IsActive: Model.IsActive,
                    RowVersion: ExistingStation?.RowVersion);

                var updated = await stationApiClient.UpdateStationAsync(Model.Code, request);
                snackbar.AddSuccess($"Station '{updated.Code}' updated successfully.");
                MudDialog.Close(DialogResult.Ok(updated));
            }
            else
            {
                var request = new CreateStationRequest(
                    Code: Model.Code,
                    Name: Model.Name,
                    City: Model.City);

                var created = await stationApiClient.CreateStationAsync(request);
                snackbar.AddSuccess($"Station '{created.Code}' created successfully.");
                MudDialog.Close(DialogResult.Ok(created));
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Failed to save station: {ex.Message}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public class StationFormModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
