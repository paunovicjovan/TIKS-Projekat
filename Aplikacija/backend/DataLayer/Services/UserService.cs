namespace DataLayer.Services;

public class UserService
{
    private readonly IMongoCollection<User> _usersCollection =
        DbConnection.GetDatabase().GetCollection<User>("users_collection");

    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly TokenService _tokenService;
    private readonly IServiceProvider _serviceProvider;

    public UserService(TokenService tokenService, IServiceProvider serviceProvider)
    {
        _tokenService = tokenService;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<AuthResponseDTO, ErrorMessage>> Register(CreateUserDTO userDto)
    {
        try
        {
            string usernamePattern = @"^[a-zA-Z0-9._]+$";
            Regex usernameRegex = new Regex(usernamePattern);

            if (!usernameRegex.IsMatch(userDto.Username))
                return
                    "Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i ."
                        .ToError();

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            Regex emailRegex = new Regex(emailPattern);

            if (!emailRegex.IsMatch(userDto.Email))
                return "E-mail nije u ispravnom formatu.".ToError();

            var existingUserByUsername = await _usersCollection
                .Find(u => u.Username == userDto.Username)
                .FirstOrDefaultAsync();

            if (existingUserByUsername != null)
                return "Već postoji korisnik sa unetim korisničkim imenom.".ToError();

            var existingUserByEmail = await _usersCollection
                .Find(u => u.Email == userDto.Email)
                .FirstOrDefaultAsync();

            if (existingUserByEmail != null)
                return "Već postoji korisnik sa unetim e-mail-om.".ToError();

            var newUser = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PhoneNumber = userDto.PhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(null!, userDto.Password),
                Role = UserRole.User
            };

            await _usersCollection.InsertOneAsync(newUser);

            return new AuthResponseDTO
            {
                Id = newUser.Id!,
                Username = newUser.Username,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber,
                Role = UserRole.User,
                Token = _tokenService.CreateToken(newUser)
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom registracije korisnika.".ToError();
        }
    }

    public async Task<Result<AuthResponseDTO, ErrorMessage>> Login(LoginRequestDTO request)
    {
        try
        {
            var user = await _usersCollection
                .Find(u => u.Email == request.Email)
                .FirstOrDefaultAsync();

            if (user == null)
                return "Neispravan email ili lozinka.".ToError(403);

            var verificationResult = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash!, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return "Neispravan email ili lozinka.".ToError(403);

            var accessToken = _tokenService.CreateToken(user);

            return new AuthResponseDTO
            {
                Id = user.Id!,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Token = accessToken,
                Role = user.Role
            };
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom prijavljivanja.".ToError();
        }
    }

    public Result<string, ErrorMessage> GetCurrentUserId(ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
                return userId;

            return "Došlo je do greške prilikom učitavanja korisnika.".ToError();
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom učitavanja korisnika.".ToError();
        }
    }

    public async Task<Result<UserResultDTO, ErrorMessage>> GetCurrentUser(ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
                return await GetById(userId);

            return "Došlo je do greške prilikom učitavanja korisnika.".ToError();
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom učitavanja korisnika.".ToError();
        }
    }

    public async Task<Result<UserResultDTO, ErrorMessage>> GetById(string id)
    {
        try
        {
            var user = await _usersCollection
                .Find(u => u.Id == id)
                .FirstOrDefaultAsync();

            if (user == null)
                return "Korisnik nije pronađen.".ToError(404);

            return new UserResultDTO(user);
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja podataka o korisniku.".ToError();
        }
    }

    public async Task<Result<UserResultDTO, ErrorMessage>> Update(string userId, UpdateUserDTO userDto)
    {
        try
        {
            var existingUser = await _usersCollection
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (existingUser == null)
                return "Korisnik nije pronađen.".ToError(404);

            existingUser.Username = userDto.Username;
            existingUser.PhoneNumber = userDto.PhoneNumber;

            var updateUserResult = await _usersCollection.ReplaceOneAsync(u => u.Id == userId, existingUser);

            if (updateUserResult.ModifiedCount == 0)
                return "Neuspešna izmena podataka".ToError();

            return new UserResultDTO(existingUser);
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom preuzimanja podataka o korisniku.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> AddCommentToUser(string userId, string commentId)
    {
        try
        {
            var updateResult = await _usersCollection.UpdateOneAsync(
                u => u.Id == userId,
                Builders<User>.Update.Push(u => u.CommentIds, commentId)
            );

            if (updateResult.ModifiedCount == 0)
            {
                return "Korisnik nije pronađen ili nije ažuriran.".ToError();
            }

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom dodavanja komentara korisniku.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> RemoveCommentFromUser(string userId, string commentId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Pull(u => u.CommentIds, commentId);

            var updateResult = await _usersCollection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
                return "Komentar nije pronađen kod korisnika.".ToError();

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom uklanjanja korisnikovog komentara.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> AddPostToUser(string userId, string postId)
    {
        try
        {
            var updateResult = await _usersCollection.UpdateOneAsync(
                u => u.Id == userId,
                Builders<User>.Update.Push(u => u.PostIds, postId)
            );

            if (updateResult.ModifiedCount == 0)
            {
                return "Korisnik nije pronađen ili nije ažuriran.".ToError();
            }

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom dodavanja objave korisniku.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> RemovePostFromUser(string userId, string postId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Pull(u => u.PostIds, postId);

            var updateResult = await _usersCollection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
                return "Objava nije pronađena kod korisnika.".ToError();

            return true;
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom uklanjanja korisnikove objave.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> AddFavoriteEstate(string userId, string estateId)
    {
        try
        {
            var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (user.FavoriteEstateIds.Contains(estateId))
            {
                return "Nekretnina je već u omiljenim.".ToError();
            }

            user.FavoriteEstateIds.Add(estateId);

            var updateResult = await _usersCollection.ReplaceOneAsync(
                x => x.Id == userId,
                user
            );
            
            var estateService = _serviceProvider.GetRequiredService<EstateService>();

            var updateEstateResult = await estateService.AddFavoriteUserToEstate(estateId, userId);
            
            if(updateEstateResult.IsError)
                return updateEstateResult.Error;

            if (updateResult.ModifiedCount > 0)
            {
                return true;
            }

            return "Došlo je do greške prilikom ažuriranja omiljenih nekretnina.".ToError();
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom dodavanja nekretnine u omiljene.".ToError();
        }
    }

    public async Task<Result<bool, ErrorMessage>> RemoveFavoriteEstate(string userId, string estateId)
    {
        try
        {
            var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (!user.FavoriteEstateIds.Contains(estateId))
            {
                return "Nekretnina se ne nalazi u omiljenim.".ToError();
            }

            user.FavoriteEstateIds.Remove(estateId);

            var updateResult = await _usersCollection.ReplaceOneAsync(
                x => x.Id == userId,
                user
            );
            
            var estateService = _serviceProvider.GetRequiredService<EstateService>();

            var updateEstateResult = await estateService.RemoveFavoriteUserFromEstate(estateId, userId);
            
            if(updateEstateResult.IsError)
                return updateEstateResult.Error;

            if (updateResult.ModifiedCount > 0)
            {
                return true;
            }

            return "Došlo je do greške prilikom ažuriranja omiljenih nekretnina.".ToError();
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom uklanjanja nekretnine iz omiljenih.".ToError();
        }
    }
    
    public async Task<Result<bool, ErrorMessage>> CanAddToFavorite(string userId, string estateId)
    {
        try
        {
            var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                return "Korisnik nije pronađen.".ToError();
            }

            var estateService = _serviceProvider.GetRequiredService<EstateService>();

            (bool isError, var estate, ErrorMessage? error) = await estateService.GetEstate(estateId);

            if (isError)
                return error!;

            if (estate!.User!.Id == userId)
                return false;

            return !user.FavoriteEstateIds.Contains(estateId);
        }
        catch (Exception)
        {
            return "Došlo je do greške prilikom određivanja da li korisnik može da doda nekretninu u omiljene.".ToError();
        }
    }
}