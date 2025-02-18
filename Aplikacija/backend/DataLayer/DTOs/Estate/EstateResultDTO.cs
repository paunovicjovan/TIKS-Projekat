namespace DataLayer.DTOs.Estate;

public class EstateResultDTO
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required double Price { get; set; }
    public required int SquareMeters  { get; set; }
    public required int TotalRooms { get; set; }
    public required EstateCategory Category { get; set; }
    public int? FloorNumber { get; set; }
    public required List<string> Images { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public UserResultDTO? User { get; set; }

    [SetsRequiredMembers]
    public EstateResultDTO(BsonValue estateDocument)
    {
        Id = estateDocument["_id"].AsObjectId.ToString();
        Title = estateDocument["Title"].AsString;
        Description = estateDocument["Description"].AsString;
        Price = estateDocument["Price"].AsDouble;
        SquareMeters = estateDocument["SquareMeters"].AsInt32;
        TotalRooms = estateDocument["TotalRooms"].AsInt32;
        Category = (EstateCategory)estateDocument["Category"].AsInt32;
        FloorNumber = !estateDocument["FloorNumber"].IsBsonNull ? estateDocument["FloorNumber"].AsInt32 : (int?)null;
        Images = estateDocument["Images"].AsBsonArray.Select(img => img.AsString).ToList();
        Longitude = estateDocument["Longitude"].AsDouble;
        Latitude = estateDocument["Latitude"].AsDouble;
        User = new UserResultDTO()
        {
            Id = estateDocument["UserId"].AsObjectId.ToString(),
            // ne mogu direktno da se procitaju iz estateDocument ako nije spojen sa user
            // a required su pa mora string.Empty
            Username = string.Empty,
            Email = string.Empty,
            PhoneNumber = string.Empty
        };
    }
    
    [SetsRequiredMembers]
    public EstateResultDTO(Models.Estate estate)
    {
        Id = estate.Id!;
        Title = estate.Title;
        Description = estate.Description;
        Price = estate.Price;
        SquareMeters = estate.SquareMeters;
        TotalRooms = estate.TotalRooms;
        Category = estate.Category;
        FloorNumber = estate.FloorNumber;
        Images = [..estate.Images];
        Longitude = estate.Longitude;
        Latitude = estate.Latitude;
    }
}