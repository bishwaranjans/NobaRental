using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace NobaRental.Frontend.Client.Helpers;

internal static class SnackbarExtensions
{
#pragma warning disable IDISP004
    public static void AddError(this ISnackbar snackbar, string message)
    {
        snackbar.Add(message, Severity.Error);
    }

    public static void AddError(this ISnackbar snackbar, MarkupString message, Action<SnackbarOptions>? configure = null)
    {
        snackbar.Add(message, Severity.Error, configure);
    }

    public static void AddWarning(this ISnackbar snackbar, string message)
    {
        snackbar.Add(message, Severity.Warning);
    }

    public static void AddSuccess(this ISnackbar snackbar, string message)
    {
        snackbar.Add(message, Severity.Success);
    }

    public static void AddInfo(this ISnackbar snackbar, string message)
    {
        snackbar.Add(message, Severity.Info);
    }
#pragma warning restore IDISP004
}
