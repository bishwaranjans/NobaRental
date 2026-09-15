using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Categories;

public partial class CategoriesPage(ICarCategoryApiClient apiClient, IDialogService dialogService, ISnackbar snackbar) : ComponentBase
{
    protected List<CarCategoryResponse> Categories { get; set; } = [];
    protected bool IsLoading = true;
    protected bool IncludeInactive;

    protected override async Task OnInitializedAsync()
    {
        await LoadCategories();
    }

    protected async Task OnIncludeInactiveChanged(bool value)
    {
        IncludeInactive = value;
        await LoadCategories();
    }

    protected async Task LoadCategories()
    {
        IsLoading = true;
        try
        {
            using var response = await apiClient.GetCategories(IncludeInactive ? null : true);
            Categories = response.GetContent().ToList();
            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                snackbar.AddError(
                    ApiExceptionHelper.GetErrorMessage(response.StringContent, "Failed to load categories."));
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError($"Error loading categories: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
        finally
        {
            IsLoading = false;
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

        var dialog = await dialogService.ShowAsync<CategoryDialog>("Add Category", options);
        if ((await dialog.Result) is { Canceled: false })
            await LoadCategories();
    }

    protected async Task OpenEditDialog(CarCategoryResponse category)
    {
        var parameters = new DialogParameters<CategoryDialog>
        {
            { x => x.ExistingCategory, category }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await dialogService.ShowAsync<CategoryDialog>("Edit Category", parameters, options);
        if ((await dialog.Result) is { Canceled: false })
            await LoadCategories();
    }

    protected async Task DeleteCategory(CarCategoryResponse category)
    {
        var confirmed = await dialogService.ShowMessageBoxAsync(
            "Delete Category",
            $"Are you sure you want to delete category '{category.Code}'?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed is not true)
            return;

        try
        {
            using var response = await apiClient.DeleteCategory(category.Code);
            if (response.ResponseMessage.IsSuccessStatusCode)
            {
                snackbar.AddSuccess($"Category '{category.Code}' was removed.");
                await LoadCategories();
            }
            else
            {
                snackbar.AddError(
                    ApiExceptionHelper.GetErrorMessage(
                        response.StringContent,
                        "Could not delete category."));
            }
        }
        catch (Exception ex)
        {
            snackbar.AddError(
                $"Could not delete category: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
    }
}