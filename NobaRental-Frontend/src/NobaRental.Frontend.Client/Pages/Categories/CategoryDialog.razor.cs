using Microsoft.AspNetCore.Components;
using MudBlazor;
using NobaRental.Backend.WebApi.Client;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Frontend.Client.Helpers;

namespace NobaRental.Frontend.Client.Pages.Categories;

public partial class CategoryDialog(ICarCategoryApiClient apiClient, ISnackbar snackbar) : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public CarCategoryResponse? ExistingCategory { get; set; }

    protected bool IsEditMode => ExistingCategory is not null;
    protected MudForm Form = null!;
    protected bool IsValid;
    protected bool IsSubmitting;
    protected CategoryModel Model { get; set; } = new();

    protected override void OnInitialized()
    {
        if (ExistingCategory is not null)
        {
            Model = new CategoryModel
            {
                Code = ExistingCategory.Code,
                Name = ExistingCategory.Name,
                DayMultiplier = ExistingCategory.DayMultiplier,
                KmMultiplier = ExistingCategory.KmMultiplier,
                ChargesKilometers = ExistingCategory.ChargesKilometers,
                IsActive = ExistingCategory.IsActive
            };
        }
    }

    protected void Cancel() => MudDialog.Cancel();

    protected async Task Submit()
    {
        await Form.ValidateAsync();
        if (!Form.IsValid)
            return;

        IsSubmitting = true;
        try
        {
            CarCategoryResponse result;
            if (IsEditMode)
            {
                var request = new UpdateCarCategoryRequest(
                    Model.Name,
                    Model.DayMultiplier,
                    Model.KmMultiplier,
                    Model.ChargesKilometers,
                    Model.IsActive,
                    ExistingCategory!.RowVersion);
                var ifMatch = ExistingCategory.RowVersion is { Length: > 0 }
                    ? $"\"{Convert.ToBase64String(ExistingCategory.RowVersion)}\""
                    : null;

                using var response = await apiClient.UpdateCategory(Model.Code, request, ifMatch);
                result = response.GetContent()
                    ?? throw new InvalidOperationException(
                        ApiExceptionHelper.GetErrorMessage(
                            response.StringContent,
                            "Failed to update category."));
            }
            else
            {
                var request = new CreateCarCategoryRequest(
                    Model.Code.Trim().ToUpperInvariant(),
                    Model.Name.Trim(),
                    Model.DayMultiplier,
                    Model.KmMultiplier,
                    Model.ChargesKilometers);

                using var response = await apiClient.CreateCategory(request);
                result = response.GetContent()
                    ?? throw new InvalidOperationException(
                        ApiExceptionHelper.GetErrorMessage(
                            response.StringContent,
                            "Failed to create category."));
            }

            snackbar.AddSuccess($"Category '{result.Code}' saved successfully.");
            MudDialog.Close(DialogResult.Ok(result));
        }
        catch (Exception ex)
        {
            snackbar.AddError(
                $"Failed to save category: {ApiExceptionHelper.GetErrorMessage(ex)}");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    protected sealed class CategoryModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal DayMultiplier { get; set; } = 1m;
        public decimal KmMultiplier { get; set; } = 1m;
        public bool ChargesKilometers { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
