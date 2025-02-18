namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentController : ControllerBase
{
    private readonly CommentService _commentService;
    private readonly UserService _userService;

    public CommentController(CommentService commentService, UserService userService)
    {
        _commentService = commentService;
        _userService = userService;
    }

    [HttpPost("Create")]
    [Authorize]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentDTO commentDto)
    {
        var userResult = _userService.GetCurrentUserId(User);

        if (userResult.IsError)
        {
            return StatusCode(userResult.Error?.StatusCode ?? 400, userResult.Error?.Message);
        }

        (bool isError, var response, ErrorMessage? error) =
            await _commentService.CreateComment(commentDto, userResult.Data);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }

    [HttpGet("GetCommentsForPost/{postId}")]
    [Authorize]
    public async Task<IActionResult> GetCommentsForPost([FromRoute] string postId, [FromQuery] int? skip,
        [FromQuery] int? limit)
    {
        (bool isError, var response, ErrorMessage? error) =
            await _commentService.GetCommentsForPost(postId, skip ?? 0, limit ?? 10);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }
    
    [HttpPut("Update/{commentId}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] string commentId, [FromBody] UpdateCommentDTO commentDto)
    {
        (bool isError, var response, ErrorMessage? error) =
            await _commentService.UpdateComment(commentId, commentDto);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return Ok(response);
    }
    
    [HttpDelete("Delete/{commentId}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] string commentId)
    {
        (bool isError, var response, ErrorMessage? error) =
            await _commentService.DeleteComment(commentId);

        if (isError)
        {
            return StatusCode(error?.StatusCode ?? 400, error?.Message);
        }

        return NoContent();
    }
}