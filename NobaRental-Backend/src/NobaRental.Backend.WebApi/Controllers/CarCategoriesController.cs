using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.WebApi.Auth;
using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Mapping;

namespace NobaRental.Backend.WebApi.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CarCategoriesController(ICarCategoryApi categoryApi) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<IReadOnlyCollection<CarCategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CarCategoryResponse>>> GetAllCategories(
        [FromQuery] bool? onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var categories = await categoryApi.GetCategories(onlyActive, cancellationToken);
        return Ok(categories.MapToResponse());
    }

    [HttpGet("{code}")]
    [Authorize(Policy = AuthConstants.Policies.RentalsRead)]
    [ProducesResponseType<CarCategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarCategoryResponse>> GetCategoryByCode(
        string code,
        CancellationToken cancellationToken = default)
    {
        var category = await categoryApi.GetCategory(code, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return Ok(category.MapToResponse());
    }

    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.FleetManage)]
    [ProducesResponseType<CarCategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CarCategoryResponse>> CreateCategory(
        [FromBody] CreateCarCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await categoryApi.CreateCategory(
            request.Code,
            request.Name,
            request.DayMultiplier,
            request.KmMultiplier,
            request.ChargesKilometers,
            cancellationToken);

        var response = category.MapToResponse();
        return CreatedAtAction(nameof(GetCategoryByCode), new { code = response.Code }, response);
    }

    [HttpPut("{code}")]
    [Authorize(Policy = AuthConstants.Policies.FleetManage)]
    [ProducesResponseType<CarCategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CarCategoryResponse>> UpdateCategory(
        string code,
        [FromBody] UpdateCarCategoryRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch = null,
        CancellationToken cancellationToken = default)
    {
        var rowVersion = request.RowVersion ?? [];
        if (rowVersion.Length == 0 && !string.IsNullOrWhiteSpace(ifMatch))
        {
            var match = ifMatch.Trim('"');
            rowVersion = Convert.FromBase64String(match);
        }

        var updated = await categoryApi.UpdateCategory(
            code,
            request.Name,
            request.DayMultiplier,
            request.KmMultiplier,
            request.ChargesKilometers,
            request.IsActive,
            rowVersion,
            cancellationToken);

        return Ok(updated.MapToResponse());
    }

    [HttpDelete("{code}")]
    [Authorize(Policy = AuthConstants.Policies.FleetManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(
        string code,
        CancellationToken cancellationToken = default)
    {
        await categoryApi.DeleteCategory(code, cancellationToken);
        return NoContent();
    }
}
