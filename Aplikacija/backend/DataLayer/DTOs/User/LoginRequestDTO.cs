namespace DataLayer.DTOs.User;

public class LoginRequestDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
