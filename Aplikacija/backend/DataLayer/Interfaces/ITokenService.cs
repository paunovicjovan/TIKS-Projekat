namespace DataLayer.Interfaces;

public interface ITokenService
{
    public string CreateToken(User user);
}