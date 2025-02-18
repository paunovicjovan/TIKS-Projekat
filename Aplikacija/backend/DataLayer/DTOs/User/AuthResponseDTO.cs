namespace DataLayer.DTOs.User;

public class AuthResponseDTO
{
    public required string Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Token { get; set; }
    public UserRole Role { get; set; }
}
