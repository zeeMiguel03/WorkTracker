using Application.DTOs.User;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Application.Tests.Services;

public class UserServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const string ValidPassword = "StrongPassword123!";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IExternalLoginRepository _externalLoginRepository = Substitute.For<IExternalLoginRepository>();
    private readonly IProductSaleRepository _productSaleRepository = Substitute.For<IProductSaleRepository>();
    private readonly IUploadService _uploadService = Substitute.For<IUploadService>();
    private readonly IPasswordHasher<User> _passwordHasher = Substitute.For<IPasswordHasher<User>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _currentUserService.GetUserId().Returns(UserId);
        _currentUserService.GetUserIdOrNull().Returns(UserId);
        _passwordHasher.HashPassword(Arg.Any<User>(), Arg.Any<string>()).Returns("hashed-password");
        _sut = new UserService(
            _userRepository,
            _externalLoginRepository,
            _productSaleRepository,
            _uploadService,
            _passwordHasher,
            _currentUserService,
            _unitOfWork,
            NullLogger<UserService>.Instance);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidData_CreatesUserWithDefaultStatusesAndHashedPassword()
    {
        var dto = CreateUserDto();
        _userRepository.EmailExistsAsync(dto.Email, Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        await _sut.CreateUserAsync(dto);

        await _userRepository.Received(1).AddAsync(Arg.Is<User>(user =>
            user.Name == dto.Name && user.Email == dto.Email && user.PasswordHash == "hashed-password" &&
            user.UtCreation == UserId && user.TasksStatus.Count == 4 &&
            user.TasksStatus.Select(status => status.Name).SequenceEqual(
                new[] { "A fazer", "Em progresso", "Em revisão", "Concluídas" })), Arg.Any<CancellationToken>());
        _passwordHasher.Received(1).HashPassword(Arg.Any<User>(), ValidPassword);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUserAsync_WithProfileImage_UploadsAndPersistsImagePath()
    {
        var dto = CreateUserDto();
        dto.ProfileImageUrl = Substitute.For<IFormFile>();
        _userRepository.EmailExistsAsync(dto.Email, Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _uploadService.UploadUserImageAsync(dto.ProfileImageUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("users/profile.png"));

        await _sut.CreateUserAsync(dto);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(user => user.ProfileImageUrl == "users/profile.png"), Arg.Any<CancellationToken>());
        await _uploadService.Received(1).UploadUserImageAsync(dto.ProfileImageUrl, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailAlreadyExists_ThrowsBeforeHashingOrUploading()
    {
        var dto = CreateUserDto();
        dto.ProfileImageUrl = Substitute.For<IFormFile>();
        _userRepository.EmailExistsAsync(dto.Email, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateUserAsync(dto));

        Assert.Equal("EMAIL_ALREADY_EXISTS", exception.Code);
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<User>(), Arg.Any<string>());
        await _uploadService.DidNotReceive().UploadUserImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "PASSWORD_REQUIRED")]
    [InlineData("Aa1!short", "PASSWORD_TOO_SHORT")]
    [InlineData("Aa1!aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "PASSWORD_TOO_LONG")]
    [InlineData("PASSWORD123!", "PASSWORD_LOWERCASE_REQUIRED")]
    [InlineData("password123!", "PASSWORD_UPPERCASE_REQUIRED")]
    [InlineData("Password!!!!", "PASSWORD_DIGIT_REQUIRED")]
    [InlineData("Password1234", "PASSWORD_SYMBOL_REQUIRED")]
    public async Task CreateUserAsync_WhenPasswordDoesNotMeetRequirements_ThrowsWithoutCreatingUser(
        string password, string expectedCode)
    {
        var dto = CreateUserDto();
        dto.Password = password;

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateUserAsync(dto));

        Assert.Equal(expectedCode, exception.Code);
        await _userRepository.DidNotReceive().EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<User>(), Arg.Any<string>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUserAsync_WhenSavingFails_DeletesUploadedProfileImageAndRethrows()
    {
        var dto = CreateUserDto();
        dto.ProfileImageUrl = Substitute.For<IFormFile>();
        _userRepository.EmailExistsAsync(dto.Email, Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _uploadService.UploadUserImageAsync(dto.ProfileImageUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("users/orphan.png"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("database failure"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateUserAsync(dto));

        Assert.Equal("database failure", exception.Message);
        await _uploadService.Received(1).DeleteImageAsync("users/orphan.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddExternalUserAsync_WhenNameIsBlank_UsesEmailPrefixAndCreatesDefaultStatuses()
    {
        _userRepository.EmailExistsAsync("person@example.com", Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var user = await _sut.AddExternalUserAsync("   ", "person@example.com");

        Assert.Equal("person", user.Name);
        Assert.Equal("person@example.com", user.Email);
        Assert.Null(user.PasswordHash);
        Assert.Equal(4, user.TasksStatus.Count);
        await _userRepository.Received(1).AddAsync(user, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddExternalUserAsync_TrimsNameAndLimitsItTo150Characters()
    {
        var longName = $"  {new string('N', 155)}  ";
        _userRepository.EmailExistsAsync("person@example.com", Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var user = await _sut.AddExternalUserAsync(longName, "person@example.com");

        Assert.Equal(150, user.Name.Length);
        Assert.Equal(new string('N', 150), user.Name);
    }

    [Fact]
    public async Task AddExternalUserAsync_WhenEmailAlreadyExists_ThrowsWithoutAddingUser()
    {
        _userRepository.EmailExistsAsync("person@example.com", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _sut.AddExternalUserAsync("Person", "person@example.com"));

        Assert.Equal("EMAIL_ALREADY_EXISTS", exception.Code);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserAsync_WithoutNewImage_KeepsExistingImageAndSavesChanges()
    {
        var user = CreateExistingUser(imagePath: "users/current.png");
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        await _sut.UpdateUserAsync(new UpdateUserDTO { Name = " Updated Name " });

        Assert.Equal("Updated Name", user.Name);
        Assert.Equal("users/current.png", user.ProfileImageUrl);
        _userRepository.Received(1).Update(user);
        await _uploadService.DidNotReceive().UploadUserImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserAsync_WithNewImage_ReplacesImageAndDeletesOldAfterSave()
    {
        var user = CreateExistingUser(imagePath: "users/old.png");
        var image = Substitute.For<IFormFile>();
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _uploadService.UploadUserImageAsync(image, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("users/new.png"));

        await _sut.UpdateUserAsync(new UpdateUserDTO { Name = "New Name", ProfileImageUrl = image });

        Assert.Equal("users/new.png", user.ProfileImageUrl);
        await _uploadService.Received(1).DeleteImageAsync("users/old.png", Arg.Any<CancellationToken>());
        _userRepository.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserAsync_WhenSavingNewImageFails_DeletesNewImageAndRethrows()
    {
        var user = CreateExistingUser(imagePath: "users/old.png");
        var image = Substitute.For<IFormFile>();
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _uploadService.UploadUserImageAsync(image, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("users/new.png"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("database failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateUserAsync(new UpdateUserDTO { Name = "New Name", ProfileImageUrl = image }));

        await _uploadService.Received(1).DeleteImageAsync("users/new.png", Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync("users/old.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserDoesNotExist_ThrowsWithoutUploadingOrSaving()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));
        var image = Substitute.For<IFormFile>();

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _sut.UpdateUserAsync(new UpdateUserDTO { Name = "New Name", ProfileImageUrl = image }));

        Assert.Equal("USER_NOT_FOUND", exception.Code);
        await _uploadService.DidNotReceive().UploadUserImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeEmailAsync_WithValidCurrentPassword_ChangesEmailAndInvalidatesTokens()
    {
        var user = CreateExistingUser(passwordHash: "existing-hash");
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _passwordHasher.VerifyHashedPassword(user, "existing-hash", "CurrentPassword123!")
            .Returns(PasswordVerificationResult.Success);
        _userRepository.GetByEmailAsync(" NEW@EXAMPLE.COM ", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));
        var tokenVersion = user.TokenVersion;

        await _sut.ChangeEmailAsync(new ChangeUserEmailDTO
        {
            currentPassword = "CurrentPassword123!",
            newEmail = " NEW@EXAMPLE.COM "
        });

        Assert.Equal("new@example.com", user.Email);
        Assert.Equal(tokenVersion + 1, user.TokenVersion);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenCurrentPasswordIsInvalid_ThrowsWithoutChangingEmail()
    {
        var user = CreateExistingUser(passwordHash: "existing-hash");
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _passwordHasher.VerifyHashedPassword(user, "existing-hash", "wrong")
            .Returns(PasswordVerificationResult.Failed);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.ChangeEmailAsync(
            new ChangeUserEmailDTO { currentPassword = "wrong", newEmail = "new@example.com" }));

        Assert.Equal("INVALID_PASSWORD", exception.Code);
        Assert.Equal("user@example.com", user.Email);
        await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenEmailBelongsToAnotherUser_ThrowsWithoutChangingEmail()
    {
        var user = CreateExistingUser(passwordHash: "existing-hash");
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _passwordHasher.VerifyHashedPassword(user, "existing-hash", ValidPassword)
            .Returns(PasswordVerificationResult.Success);
        _userRepository.GetByEmailAsync("other@example.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(CreateExistingUser(OtherUserId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ChangeEmailAsync(
            new ChangeUserEmailDTO { currentPassword = ValidPassword, newEmail = "other@example.com" }));

        Assert.Equal("EMAIL_ALREADY_EXISTS", exception.Code);
        Assert.Equal("user@example.com", user.Email);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ChangeEmailAsync(
            new ChangeUserEmailDTO { currentPassword = ValidPassword, newEmail = "new@example.com" }));

        Assert.Equal("USER_NOT_FOUND", exception.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ChangesHashAndInvalidatesTokens()
    {
        var user = CreateExistingUser(passwordHash: "old-hash");
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _passwordHasher.VerifyHashedPassword(user, "old-hash", "OldPassword123!")
            .Returns(PasswordVerificationResult.Success);
        _passwordHasher.HashPassword(user, "NewPassword123!").Returns("new-hash");
        var tokenVersion = user.TokenVersion;

        await _sut.ChangePasswordAsync(new ChangeUserPasswordDTO
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        });

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal(tokenVersion + 1, user.TokenVersion);
        _passwordHasher.Received(1).HashPassword(user, "NewPassword123!");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_ForExternalUserWithoutCurrentPassword_SetsLocalPassword()
    {
        var user = CreateExistingUser(passwordHash: null);
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _passwordHasher.HashPassword(user, "NewPassword123!").Returns("new-hash");

        await _sut.ChangePasswordAsync(new ChangeUserPasswordDTO { NewPassword = "NewPassword123!" });

        Assert.Equal("new-hash", user.PasswordHash);
        _passwordHasher.DidNotReceive().VerifyHashedPassword(
            Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCurrentPasswordIsWrong_ThrowsWithoutChangingHash()
    {
        var user = CreateExistingUser(passwordHash: "old-hash");
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _passwordHasher.VerifyHashedPassword(user, "old-hash", "wrong")
            .Returns(PasswordVerificationResult.Failed);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.ChangePasswordAsync(
            new ChangeUserPasswordDTO { CurrentPassword = "wrong", NewPassword = "NewPassword123!" }));

        Assert.Equal("INVALID_PASSWORD", exception.Code);
        Assert.Equal("old-hash", user.PasswordHash);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenExternalUserProvidesCurrentPassword_ThrowsUnauthorized()
    {
        var user = CreateExistingUser(passwordHash: null);
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.ChangePasswordAsync(
            new ChangeUserPasswordDTO { CurrentPassword = "unexpected", NewPassword = "NewPassword123!" }));

        Assert.Equal("INVALID_PASSWORD", exception.Code);
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<User>(), Arg.Any<string>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenNewPasswordIsBlank_ThrowsWithoutSaving()
    {
        var user = CreateExistingUser(passwordHash: null);
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _sut.ChangePasswordAsync(new ChangeUserPasswordDTO()));

        Assert.Equal("PASSWORD_REQUIRED", exception.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ChangePasswordAsync(
            new ChangeUserPasswordDTO { NewPassword = "NewPassword123!" }));

        Assert.Equal("USER_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesSalesAndUserThenDeletesProfileImage()
    {
        var user = CreateExistingUser(imagePath: "users/profile.png");
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        await _sut.DeleteUserAsync();

        await _productSaleRepository.Received(1).RemoveByUserAsync(UserId, Arg.Any<CancellationToken>());
        _userRepository.Received(1).Remove(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.Received(1).DeleteImageAsync("users/profile.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserHasNoImage_DoesNotDeleteImage()
    {
        var user = CreateExistingUser();
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        await _sut.DeleteUserAsync();

        await _productSaleRepository.Received(1).RemoveByUserAsync(UserId, Arg.Any<CancellationToken>());
        _userRepository.Received(1).Remove(user);
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserDoesNotExist_ThrowsWithoutDeletingSales()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteUserAsync());

        Assert.Equal("USER_NOT_FOUND", exception.Code);
        await _productSaleRepository.DidNotReceive().RemoveByUserAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _userRepository.DidNotReceive().Remove(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenEmailBelongsToCurrentUser_ReturnsMappedUser()
    {
        var user = CreateExistingUser(imagePath: "users/profile.png", passwordHash: "local-hash");
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _externalLoginRepository.HasProviderAsync(UserId, "Google", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var result = await _sut.GetUserByEmailAsync(user.Email);

        Assert.NotNull(result);
        Assert.Equal(UserId, result.Id);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.ProfileImageUrl, result.ProfileImageUrl);
        Assert.Equal(user.CreatedAt, result.CreatedAt);
        Assert.Equal(user.UtCreation, result.UtCreation);
        Assert.True(result.HasLocalPassword);
        Assert.True(result.HasGoogleLogin);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        _userRepository.GetByEmailAsync("missing@example.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetUserByEmailAsync("missing@example.com"));

        Assert.Equal("USER_NOT_FOUND", exception.Code);
        await _externalLoginRepository.DidNotReceive().HasProviderAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenEmailBelongsToAnotherUser_ThrowsForbidden()
    {
        var user = CreateExistingUser(OtherUserId);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetUserByEmailAsync(user.Email));

        Assert.Equal("FORBIDDEN", exception.Code);
        await _externalLoginRepository.DidNotReceive().HasProviderAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsMappedUser()
    {
        var user = CreateExistingUser(passwordHash: null);
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _externalLoginRepository.HasProviderAsync(UserId, "Google", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var result = await _sut.GetUserByIdAsync();

        Assert.NotNull(result);
        Assert.Equal(UserId, result.Id);
        Assert.False(result.HasLocalPassword);
        Assert.False(result.HasGoogleLogin);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetUserByIdAsync());

        Assert.Equal("USER_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task GetProfileImageAsync_WhenImageExists_ReturnsImageStream()
    {
        var user = CreateExistingUser(imagePath: "users/profile.png");
        using var stream = new MemoryStream([1, 2, 3]);
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        _uploadService.ReadUploadAsync("users/profile.png", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(stream));

        var result = await _sut.GetProfileImageAsync();

        Assert.Same(stream, result);
        await _uploadService.Received(1).ReadUploadAsync("users/profile.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProfileImageAsync_WhenImageDoesNotExist_ReturnsNullWithoutReading()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(CreateExistingUser()));

        var result = await _sut.GetProfileImageAsync();

        Assert.Null(result);
        await _uploadService.DidNotReceive().ReadUploadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProfileImageAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetProfileImageAsync());

        Assert.Equal("USER_NOT_FOUND", exception.Code);
        await _uploadService.DidNotReceive().ReadUploadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static CreateUserDTO CreateUserDto() => new()
    {
        Name = "Test User",
        Email = "user@example.com",
        Password = ValidPassword
    };

    private static User CreateExistingUser(int id = UserId, string? imagePath = null, string? passwordHash = "password-hash")
    {
        var user = User.Create("Test User", id == UserId ? "user@example.com" : "other@example.com", imagePath, id);
        SetUserId(user, id);
        if (passwordHash is not null)
        {
            user.SetPasswordHash(passwordHash);
        }

        return user;
    }

    private static void SetUserId(User user, int id) =>
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
}
