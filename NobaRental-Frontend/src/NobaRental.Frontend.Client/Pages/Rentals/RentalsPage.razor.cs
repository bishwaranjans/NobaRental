using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Rentals;

public partial class RentalsPage(
    IRentalBookingApiClient apiClient,
    IStationApiClient stationApiClient,
    IDialogService dialogService,
    ISnackbar snackbar) : ComponentBase
{
    protected MudTable<RentalBookingResponse>? Table;
    protected string SearchString = string.Empty;
    protected RentalStatusDto? FilterStatus;
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

    protected async Task<TableData<RentalBookingResponse>> ServerReload(TableState state, CancellationToken token)
    {
        try
        {
            var stationParam = string.IsNullOrWhiteSpace(FilterStation) ? null : FilterStation;
            var searchParam = string.IsNullOrWhiteSpace(SearchString) ? null : SearchString;

            using var response = await apiClient.GetBookings(
                pageNumber: state.Page + 1,
                pageSize: state.PageSize,
                searchTerm: searchParam,
                stationCode: stationParam,
                status: FilterStatus,
                sortBy: state.SortLabel,
                sortDescending: state.SortDirection == SortDirection.Descending,
                cancellationToken: token);

            if (response.ResponseMessage.IsSuccessStatusCode && response.GetContent() is { } paged)
            {
                return new TableData<RentalBookingResponse>
                {
                    TotalItems = paged.TotalCount,
                    Items = paged.Items
                };
            }

            var error = ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to retrieve rentals data.");
            snackbar.AddWarning(error);
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error loading rentals: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }

        return new TableData<RentalBookingResponse> { TotalItems = 0, Items = [] };
    }

    protected Task SetStatusFilter(RentalStatusDto? status)
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

    protected async Task OpenPickupDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        var dialog = await dialogService.ShowAsync<PickupDialog>("Register Car Pickup", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: RentalBookingResponse })
        {
            await ReloadTable();
        }
    }

    protected async Task OpenReturnDialog(RentalBookingResponse booking)
    {
        var parameters = new DialogParameters<ReturnDialog>
        {
            { x => x.Booking, booking }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await dialogService.ShowAsync<ReturnDialog>("Register Car Return", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: RentalBookingResponse returnedBooking })
        {
            await ReloadTable();
            await OpenReceiptDialog(returnedBooking);
        }
    }

    protected async Task OpenReceiptDialog(RentalBookingResponse booking)
    {
        var parameters = new DialogParameters<ReturnReceiptDialog>
        {
            { x => x.Booking, booking }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        await dialogService.ShowAsync<ReturnReceiptDialog>("Rental Return Receipt", parameters, options);
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
}
