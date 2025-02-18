namespace DataLayer.DTOs.User;

public class UserResultDTO
{
    public required string Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    
    public UserResultDTO()
    {
        
    }
    
    [SetsRequiredMembers]
    public UserResultDTO(Models.User user)
    {
        this.Id = user.Id!;
        this.Username = user.Username;
        this.Email = user.Email;
        this.PhoneNumber = user.PhoneNumber;
        this.Role = user.Role;
    }

    [SetsRequiredMembers]
    public UserResultDTO(BsonValue userDocument)
    {
        Id = userDocument["_id"].AsObjectId.ToString();
        Username = userDocument["Username"].AsString;
        Email = userDocument["Email"].AsString;
        PhoneNumber = userDocument["PhoneNumber"].AsString;
        Role = (UserRole)userDocument["Role"].AsInt32;
    }
}