using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Cars;

public partial class CarsPage(
    ICarApiClient carApiClient,
    IStationApiClient stationApiClient,
    IDialogService dialogService,
    ISnackbar snackbar) : ComponentBase
{
    protected MudTable<CarResponse>? Table;
    protected string SearchString = string.Empty;
    protected CarStatusDto? FilterStatus;
    protected string FilterStation = string.Empty;
    protected List<StationResponse> Stations = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var stations = await stationApiClient.GetAllStationsAsync();
            Stations = stations.ToList();
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Could not load stations: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
    }

    protected async Task<TableData<CarResponse>> ServerReload(TableState state, CancellationToken token)
    {
        try
        {
            var stationParam = string.IsNullOrWhiteSpace(FilterStation) ? null : FilterStation;
            var searchParam = string.IsNullOrWhiteSpace(SearchString) ? null : SearchString;

            using var response = await carApiClient.GetCars(
                pageNumber: state.Page + 1,
                pageSize: state.PageSize,
                searchTerm: searchParam,
                stationCode: stationParam,
                status: FilterStatus,
                sortBy: state.SortLabel,
                sortDescending: state.SortDirection == SortDirection.Descending,
                cancellationToken: token);

            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } content)
            {
                return new TableData<CarResponse>
                {
                    TotalItems = content.TotalCount,
                    Items = content.Items
                };
            }

            var error = ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to retrieve fleet data.");
            snackbar.AddWarning(error);
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error loading fleet: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }

        return new TableData<CarResponse> { TotalItems = 0, Items = [] };
    }

    protected Task SetStatusFilter(CarStatusDto? status)
    {
        FilterStatus = status;
        return ReloadTable();
    }

    protected Task SetStationFilter(string stationCode)
    {
        FilterStation = stationCode;
        return ReloadTable();
    }

    protected Task OnSearchClear()
    {
        SearchString = string.Empty;
        return ReloadTable();
    }

    protected Task ReloadTable()
    {
        return Table is not null ? Table.ReloadServerData() : Task.CompletedTask;
    }

    protected async Task DecommissionCar(CarResponse car)
    {
        var confirmed = await dialogService.ShowMessageBoxAsync(
            "Decommission Vehicle",
            $"Are you sure you want to decommission '{car.RegistrationNumber}' from the fleet?",
            yesText: "Decommission",
            cancelText: "Cancel");

        if (confirmed is not true)
        {
            return;
        }

        try
        {
            using var response = await carApiClient.DeleteCar(car.RegistrationNumber);
            if (response.ResponseMessage.IsSuccessStatusCode)
            {
                snackbar.AddSuccess($"Vehicle '{car.RegistrationNumber}' decommissioned.");
                await ReloadTable();
            }
            else
            {
                snackbar.AddError(ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to decommission vehicle."));
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error: {ApiExceptionHelper.GetErrorMessage(ex)}");
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

        if (result is { Canceled: false, Data: CarResponse })
        {
            await ReloadTable();
        }
    }

    protected static Color GetCategoryColor(CarCategoryDto category) => category switch
    {
        CarCategoryDto.SmallCar => Color.Primary,
        CarCategoryDto.Combi => Color.Secondary,
        CarCategoryDto.Truck => Color.Tertiary,
        _ => Color.Default
    };

    protected static string GetCategoryName(CarCategoryDto category) => category switch
    {
        CarCategoryDto.SmallCar => "Small car",
        CarCategoryDto.Combi => "Combi",
        CarCategoryDto.Truck => "Truck",
        _ => category.ToString()
    };

    protected static Color GetStatusColor(CarStatusDto status) => status switch
    {
        CarStatusDto.Available => Color.Success,
        CarStatusDto.Rented => Color.Warning,
        CarStatusDto.Maintenance => Color.Dark,
        CarStatusDto.Decommissioned => Color.Error,
        _ => Color.Default
    };
}
