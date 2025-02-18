namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] CreateUserDTO userDto)
    {
        (bool isError, var response, ErrorMessage? error) = await _userService.Register(userDto);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        (bool isError, var response, ErrorMessage? error) = await _userService.Login(request);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpGet("GetUserById/{id}")]
    public async Task<IActionResult> GetUserById([FromRoute] string id)
    {
        (bool isError, var response, ErrorMessage? error) = await _userService.GetById(id);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpPut("Update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateUserDTO userDto)
    {
        var userResult = _userService.GetCurrentUserId(User);

        if (userResult.IsError)
        {
            return StatusCode(userResult.Error?.StatusCode ?? 400, userResult.Error?.Message);
        }

        (bool isError, var response, ErrorMessage? error) = await _userService.Update(userResult.Data, userDto);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpPost("AddToFavorites/{estateId}")]
    [Authorize]
    public async Task<IActionResult> AddToFavorites(string estateId)
    {
        var userResult = _userService.GetCurrentUserId(User);
        if (userResult.IsError)
        {
            return StatusCode(userResult.Error?.StatusCode ?? 400, userResult.Error?.Message);
        }

        var userId = userResult.Data;
        (bool isError, var isSuccessful, ErrorMessage? error) = await _userService.AddFavoriteEstate(userId, estateId);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(isSuccessful);
    }

    [HttpDelete("RemoveFromFavorites/{estateId}")]
    [Authorize]
    public async Task<IActionResult> RemoveFromFavorites(string estateId)
    {
        var userResult = _userService.GetCurrentUserId(User);
        if (userResult.IsError)
        {
            return StatusCode(userResult.Error?.StatusCode ?? 400, userResult.Error?.Message);
        }

        var userId = userResult.Data;
        (bool isError, var isSuccessful, ErrorMessage? error) =
            await _userService.RemoveFavoriteEstate(userId, estateId);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(isSuccessful);
    }
    
    [HttpGet("CanAddToFavorite/{estateId}")]
    [Authorize]
    public async Task<IActionResult> CanAddToFavorite(string estateId)
    {
        var userResult = _userService.GetCurrentUserId(User);
        if (userResult.IsError)
        {
            return StatusCode(userResult.Error?.StatusCode ?? 400, userResult.Error?.Message);
        }

        var userId = userResult.Data;
        (bool isError, var canAddToFavorite, ErrorMessage? error) =
            await _userService.CanAddToFavorite(userId, estateId);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(canAddToFavorite);
    }
}