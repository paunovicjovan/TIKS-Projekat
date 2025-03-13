namespace DataLayer.Services;

public class EstateService : IEstateService
{
    private readonly IMongoCollection<Estate> _estatesCollection;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IUserService _userService;
    private readonly IPostService _postService;

    public EstateService(IMongoCollection<Estate> estatesCollection, IMongoCollection<User> usersCollection,
        IUserService userService, IPostService postService)
    {
        _estatesCollection = estatesCollection;
        _usersCollection = usersCollection;
        _userService = userService;
        _postService = postService;
    }

    public async Task<Result<List<Estate>, ErrorMessage>> GetAllEstatesFromCollection()
    {
        try
        {
            var estates = await _estatesCollection.Find(_ => true).ToListAsync();
            return estates;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja nekretnina.".ToError();
        }
    }

    public async Task<Result<EstateResultDTO, ErrorMessage>> GetEstate(string id)
    {
        try
        {
            var estate = await _estatesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (estate == null)
                return "Nije pronađena nekretnina.".ToError();

            var userResult = await _userService.GetById(estate.UserId);
            if (userResult.IsError)
                return userResult.Error;

            var estateDto = new EstateResultDTO(estate)
            {
                User = userResult.Data
            };

            return estateDto;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja nekretnine.".ToError();
        }
    }

    public async Task<Result<Estate, ErrorMessage>> CreateEstate(EstateCreateDTO newEstateDto, string userId)
    {
        try
        {
            var imagePaths = new List<string>();

            if (!newEstateDto.Images.Any())
            {
                return "Nekretnina mora sadržati barem jednu sliku.".ToError();
            }

            foreach (var file in newEstateDto.Images)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EstateImages");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var filePath = Path.Combine(path, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                imagePaths.Add("/EstateImages/" + fileName);
            }

            var estate = new Estate
            {
                Title = newEstateDto.Title,
                Description = newEstateDto.Description,
                Price = newEstateDto.Price,
                SquareMeters = newEstateDto.SquareMeters,
                TotalRooms = newEstateDto.TotalRooms,
                Category = newEstateDto.Category,
                FloorNumber = newEstateDto.FloorNumber,
                Images = imagePaths,
                Longitude = newEstateDto.Longitude,
                Latitude = newEstateDto.Latitude,
                UserId = userId
            };

            await _estatesCollection.InsertOneAsync(estate);

            await _userService.AddEstateToUser(userId, estate.Id!);
            
            return estate;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom kreiranja nekretnine.".ToError();
        }
    }

    public async Task<Result<Estate, ErrorMessage>> UpdateEstate(string id, EstateUpdateDTO updatedEstate)
    {
        try
        {
            var existingEstate = await _estatesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingEstate == null)
            {
                return "Nije pronađena nekretnina.".ToError();
            }

            if (updatedEstate.Images != null && updatedEstate.Images.Any())
            {
                foreach (var existingImagePath in existingEstate.Images)
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingImagePath);
                    if (File.Exists(imagePath))
                        File.Delete(imagePath);
                }

                List<string> newImagesPaths = new List<string>();

                foreach (var file in updatedEstate.Images)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EstateImages");

                    var filePath = Path.Combine(path, fileName);

                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    newImagesPaths.Add("/EstateImages/" + fileName);
                }

                existingEstate.Images = newImagesPaths;
            }

            existingEstate.Title = updatedEstate.Title;
            existingEstate.Description = updatedEstate.Description;
            existingEstate.Price = updatedEstate.Price;
            existingEstate.SquareMeters = updatedEstate.SquareMeters;
            existingEstate.TotalRooms = updatedEstate.TotalRooms;
            existingEstate.Category = updatedEstate.Category;
            existingEstate.FloorNumber = updatedEstate.FloorNumber;
            existingEstate.Longitude = updatedEstate.Longitude;
            existingEstate.Latitude = updatedEstate.Latitude;

            await _estatesCollection.ReplaceOneAsync(x => x.Id == id, existingEstate);

            return existingEstate;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom ažuriranja nekretnine.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> RemoveEstate(string id)
    {
        try
        {
            var existingEstate = await _estatesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingEstate == null)
            {
                return "Nije pronađena nekretnina.".ToError();
            }

            foreach (var postId in existingEstate.PostIds)
            {
                await _postService.DeletePost(postId);
            }

            foreach (var favoriteUserId in existingEstate.FavoritedByUsersIds)
            {
                await _userService.RemoveFavoriteEstate(favoriteUserId, id);
            }

            await _userService.RemoveEstateFromUser(existingEstate.UserId, id);

            foreach (var imagePath in existingEstate.Images)
            {
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string fullPath = Path.Combine(webRootPath, imagePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }

            var result = await _estatesCollection.DeleteOneAsync(x => x.Id == id);
            
            if (result.DeletedCount > 0)
            {
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom brisanja nekretnine i povezanih podataka.".ToError();
        }
    }

    public async Task<Result<PaginatedResponseDTO<Estate>, ErrorMessage>> GetEstatesCreatedByUser(string userId,
        int page = 1, int pageSize = 10)
    {
        try
        {
            var estates = await _estatesCollection
                .Find(x => x.UserId == userId)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var totalUserEstatesCount = await _estatesCollection.CountDocumentsAsync(x => x.UserId == userId);

            return new PaginatedResponseDTO<Estate>
            {
                Data = estates,
                TotalLength = totalUserEstatesCount
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja nekretnina.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> AddPostToEstate(string estateId, string postId)
    {
        try
        {
            var updateResult = await _estatesCollection.UpdateOneAsync(
                e => e.Id == estateId,
                Builders<Estate>.Update.Push(e => e.PostIds, postId)
            );

            if (updateResult.ModifiedCount == 0)
            {
                return "Nekretnina nije pronađena ili nije ažurirana.".ToError();
            }

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom dodavanja objave kod nekretnine.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> RemovePostFromEstate(string estateId, string postId)
    {
        try
        {
            var filter = Builders<Estate>.Filter.Eq(e => e.Id, estateId);
            var update = Builders<Estate>.Update.Pull(e => e.PostIds, postId);

            var updateResult = await _estatesCollection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
                return "Objava nije pronađena kod nekretnine.".ToError();

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom uklanjanja objave sa nekretnine.".ToError();
        }
    }

    public async Task<Result<PaginatedResponseDTO<Estate>, ErrorMessage>> SearchEstatesFilter(
        string? title = null,
        int? priceMin = null,
        int? priceMax = null,
        List<string>? categories = null,
        int skip = 0,
        int limit = 10)
    {
        try
        {
            var filter = Builders<Estate>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(title))
                filter &= Builders<Estate>.Filter.Regex(x => x.Title, new BsonRegularExpression(title, "i"));

            if (priceMin.HasValue)
                filter &= Builders<Estate>.Filter.Gte(x => x.Price, priceMin.Value);

            if (priceMax.HasValue)
                filter &= Builders<Estate>.Filter.Lte(x => x.Price, priceMax.Value);

            if (categories != null && categories.Any())
            {
                var categoryEnums = categories
                    .Select(category =>
                        Enum.TryParse<EstateCategory>(category, true, out var parsedCategory)
                            ? parsedCategory
                            : (EstateCategory?)null)
                    .Where(c => c.HasValue)
                    .Select(c => c!.Value)
                    .ToList();

                filter &= Builders<Estate>.Filter.In(x => x.Category, categoryEnums);
            }

            var estates = await _estatesCollection
                .Find(filter)
                .ToListAsync();

            var totalCount = await _estatesCollection.CountDocumentsAsync(filter);

            return new PaginatedResponseDTO<Estate>
            {
                Data = estates.Skip(skip).Take(limit).ToList(),
                TotalLength = totalCount
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom pretrage nekretnina.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> AddFavoriteUserToEstate(string estateId, string userId)
    {
        try
        {
            var updateResult = await _estatesCollection.UpdateOneAsync(
                e => e.Id == estateId,
                Builders<Estate>.Update.Push(e => e.FavoritedByUsersIds, userId)
            );

            if (updateResult.ModifiedCount == 0)
            {
                return "Nekretnina nije pronađena ili nije ažurirana.".ToError();
            }

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom dodavanja korisnika kod omiljene nekretnine.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> RemoveFavoriteUserFromEstate(string estateId, string userId)
    {
        try
        {
            var filter = Builders<Estate>.Filter.Eq(e => e.Id, estateId);
            var update = Builders<Estate>.Update.Pull(e => e.FavoritedByUsersIds, userId);

            var updateResult = await _estatesCollection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
                return "Korisnik nije pronađen kod nekretnine.".ToError();

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom uklanjanja korisnika sa omiljene nekretnine.".ToError();
        }
    }

    public async Task<Result<PaginatedResponseDTO<Estate>, ErrorMessage>> GetUserFavoriteEstates(string userId,
        int page = 1, int pageSize = 10)
    {
        try
        {
            var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            var favoriteEstates = await _estatesCollection
                .Find(x => user.FavoriteEstateIds.Contains(x.Id!))
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new PaginatedResponseDTO<Estate>
            {
                Data = favoriteEstates,
                TotalLength = user.FavoriteEstateIds.Count
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja omiljenih nekretnina.".ToError();
        }
    }
}