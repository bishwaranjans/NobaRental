using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Stations;

public partial class StationsPage(
    IStationApiClient stationApiClient,
    IDialogService dialogService,
    ISnackbar snackbar) : ComponentBase
{
    protected List<StationResponse> Stations = [];
    protected bool IsLoading = true;
    protected bool IncludeInactive;

    protected override async Task OnInitializedAsync()
    {
        await LoadStations();
    }

    protected async Task OnIncludeInactiveChanged(bool value)
    {
        IncludeInactive = value;
        await LoadStations();
    }

    protected async Task LoadStations()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var list = await stationApiClient.GetAllStationsAsync(IncludeInactive);
            Stations = list.ToList();
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error loading stations: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected async Task OpenCreateDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await dialogService.ShowAsync<StationDialog>("Add Station", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: StationResponse })
        {
            await LoadStations();
        }
    }

    protected async Task OpenEditDialog(StationResponse station)
    {
        var parameters = new DialogParameters<StationDialog>
        {
            { x => x.ExistingStation, station }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await dialogService.ShowAsync<StationDialog>("Edit Station", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: StationResponse })
        {
            await LoadStations();
        }
    }

    protected async Task DeleteStation(StationResponse station)
    {
        var confirmed = await dialogService.ShowMessageBoxAsync(
            "Delete Station",
            $"Are you sure you want to decommission station '{station.Code}' ({station.Name})?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed is not true)
        {
            return;
        }

        try
        {
            await stationApiClient.DeleteStationAsync(station.Code);
            snackbar.AddSuccess($"Station '{station.Code}' was removed.");
            await LoadStations();
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Could not delete station: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
    }
}
