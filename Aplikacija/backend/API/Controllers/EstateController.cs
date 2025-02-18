using DataLayer.DTOs.Estate;
using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstateController : ControllerBase
{
    private readonly EstateService estateService;
    private readonly UserService userService;

    public EstateController(EstateService estate, UserService userService)
    {
        this.estateService = estate;
        this.userService = userService;
    }

    [HttpPost("CreateEstate")]
    [Authorize]
    public async Task<IActionResult> CreateEstate([FromForm] EstateCreateDTO newEstate)
    {
        var user = userService.GetCurrentUserId(User);
        if (user.IsError)
        {
            return StatusCode(400, "Failed to retrieve user ID.");
        }

        (bool isError, var response, ErrorMessage? error) = await estateService.CreateEstate(newEstate, user.Data);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(new { id = response.Id });
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        (bool isError, var response, ErrorMessage? error) = await estateService.GetAllEstatesFromCollection();
        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpGet("GetEstate/{id}")]
    public async Task<IActionResult> GetEstate(string id)
    {
        (bool isError, var response, ErrorMessage? error) = await estateService.GetEstate(id);
        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpPut("UpdateEstate/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateEstate(string id, [FromForm] EstateUpdateDTO updatedEstate)
    {
        (bool isError, var updatedEstateResponse, ErrorMessage? error) =
            await estateService.UpdateEstate(id, updatedEstate);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(updatedEstateResponse);
    }

    [HttpDelete("RemoveEstate/{id}")]
    [Authorize]
    public async Task<IActionResult> RemoveEstate(string id)
    {
        var userResult = userService.GetCurrentUserId(User);
        if (userResult.IsError)
        {
            return StatusCode(userResult.Error?.StatusCode ?? 400, userResult.Error?.Message);
        }

        (bool isError, _, ErrorMessage? error) = await estateService.RemoveEstate(id);
        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok("Estate removed");
    }

    [HttpGet("GetEstatesCreatedByUser/{userId}")]
    public async Task<IActionResult> GetEstatesCreatedByUser(string userId, [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        (bool isError, var response, ErrorMessage? error) = await estateService.GetEstatesCreatedByUser(userId, page ?? 1, pageSize ?? 10);
        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpGet("SearchEstates")]
    public async Task<IActionResult> SearchEstates(
        [FromQuery] string? title = null,
        [FromQuery] int? priceMin = null,
        [FromQuery] int? priceMax = null,
        [FromQuery] List<string>? categories = null,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 10)
    {
        (bool isError, var result, ErrorMessage? error) = await estateService.SearchEstatesFilter(
            title,
            priceMin,
            priceMax,
            categories,
            skip,
            limit);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(result);
    }

    [HttpGet("GetUserFavoriteEstates/{userId}")]
    public async Task<IActionResult> GetUserFavoriteEstates(string userId, [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        (bool isError, var response, ErrorMessage? error) =
            await estateService.GetUserFavoriteEstates(userId, page ?? 1, pageSize ?? 10);
        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }
}