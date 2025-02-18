namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly PostService _postService;
    private readonly UserService _userService;

    public PostController(PostService postService, UserService userService)
    {
        _postService = postService;
        _userService = userService;
    }

    [HttpPost("Create")]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDTO postDto)
    {
        var userResult = _userService.GetCurrentUserId(User);

        if (userResult.IsError)
        {
            return StatusCode(userResult.Error?.StatusCode ?? 400, userResult.Error?.Message);
        }

        (bool isError, var response, ErrorMessage? error) = await _postService.CreatePost(postDto, userResult.Data);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpGet("GetAll")]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] string? title, [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        (bool isError, var response, ErrorMessage? error) =
            await _postService.GetAllPosts(title ?? "", page ?? 1, pageSize ?? 1);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpGet("GetById/{postId}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] string postId)
    {
        (bool isError, var response, ErrorMessage? error) = await _postService.GetPostById(postId);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpGet("GetAllPostsForEstate/{estateId}")]
    [Authorize]
    public async Task<IActionResult> GetAllPostsForEstate([FromRoute] string estateId, [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        (bool isError, var response, ErrorMessage? error) =
            await _postService.GetAllPostsForEstate(estateId, page ?? 1, pageSize ?? 1);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpPut("Update/{postId}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] string postId, [FromBody] UpdatePostDTO postDto)
    {
        (bool isError, var response, ErrorMessage? error) = await _postService.UpdatePost(postId, postDto);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpDelete("Delete/{postId}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] string postId)
    {
        (bool isError, var response, ErrorMessage? error) = await _postService.DeletePost(postId);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return NoContent();
    }

    [HttpGet("GetUserPosts/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserPosts(string userId, [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        (bool isError, var response, ErrorMessage? error) = await _postService.GetUserPosts(userId, page ?? 1, pageSize ?? 10);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }
}