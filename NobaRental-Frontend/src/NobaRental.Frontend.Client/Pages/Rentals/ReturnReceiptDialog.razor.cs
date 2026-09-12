using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client.Models.Response;

namespace NobaRental.Frontend.Client.Pages.Rentals;

public partial class ReturnReceiptDialog : ComponentBase
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter, EditorRequired]
    public required RentalBookingResponse Booking { get; set; }

    protected void Close() => MudDialog.Close();
}
