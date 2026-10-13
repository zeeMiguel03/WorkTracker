using Application.DTOs.Source;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Application.Tests.Services;

public class SourceServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int SourceId = 20;

    private readonly ISourceRepository _sourceRepository = Substitute.For<ISourceRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUploadService _uploadService = Substitute.For<IUploadService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SourceService _sut;

    public SourceServiceTests()
    {
        _currentUserService.GetUserId().Returns(UserId);

        _sut = new SourceService(
            _sourceRepository,
            _currentUserService,
            _uploadService,
            _unitOfWork,
            NullLogger<SourceService>.Instance);
    }

    [Fact]
    public async Task CreateSourceAsync_WithoutImage_CreatesAndSavesSource()
    {
        var dto = CreateCreateDto();

        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));

        var result = await _sut.CreateSourceAsync(dto);

        Assert.Equal(UserId, result.UserId);
        Assert.Equal("Loja de teste", result.Name);
        Assert.Null(result.ImageUrl);
        Assert.Equal(dto.Link, result.Link);
        Assert.True(result.IsActive);
        Assert.Equal(UserId, result.UtCreation);

        await _sourceRepository.Received(1).AddAsync(
            Arg.Is<Source>(source => source.UserId == UserId && source.ImageUrl == null && source.IsActive),
            Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().UploadSourceImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSourceAsync_WithImage_UploadsImageAndPersistsUploadedPath()
    {
        var dto = CreateCreateDto();

        dto.ImageUrl = Substitute.For<IFormFile>();

        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));
        _uploadService.UploadSourceImageAsync(dto.ImageUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sources/new-image.png"));

        var result = await _sut.CreateSourceAsync(dto);

        Assert.Equal("sources/new-image.png", result.ImageUrl);
        await _uploadService.Received(1).UploadSourceImageAsync(dto.ImageUrl, Arg.Any<CancellationToken>());
        await _sourceRepository.Received(1).AddAsync(
            Arg.Is<Source>(source => source.ImageUrl == "sources/new-image.png"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSourceAsync_WhenNameAlreadyExists_ThrowsBeforeUploadingOrSaving()
    {
        var dto = CreateCreateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();
        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource(dto.Name, UserId, SourceId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateSourceAsync(dto));

        Assert.Equal("SOURCE_NAME_ALREADY_EXISTS", exception.Code);
        await _uploadService.DidNotReceive().UploadSourceImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _sourceRepository.DidNotReceive().AddAsync(Arg.Any<Source>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSourceAsync_WhenSavingFails_DeletesUploadedImageAndRethrows()
    {
        var dto = CreateCreateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();
        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));
        _uploadService.UploadSourceImageAsync(dto.ImageUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sources/orphan.png"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("database failure"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateSourceAsync(dto));

        Assert.Equal("database failure", exception.Message);
        await _uploadService.Received(1).DeleteImageAsync("sources/orphan.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSourceAsync_WhenUploadFails_DoesNotPersistSource()
    {
        var dto = CreateCreateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();
        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));
        _uploadService.UploadSourceImageAsync(dto.ImageUrl, Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("upload failure"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateSourceAsync(dto));

        Assert.Equal("upload failure", exception.Message);
        await _sourceRepository.DidNotReceive().AddAsync(Arg.Any<Source>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSourceAsync_WithoutNewImage_KeepsExistingImageAndSaves()
    {
        var source = CreateSource("Loja antiga", UserId, SourceId, "sources/current.png");
        var dto = CreateUpdateDto();
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));
        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));

        await _sut.UpdateSourceAsync(dto);

        Assert.Equal("Loja atualizada", source.Name);
        Assert.Equal("sources/current.png", source.ImageUrl);
        Assert.Equal(dto.Link, source.Link);
        Assert.False(source.IsActive);
        _sourceRepository.Received(1).Update(source);
        await _uploadService.DidNotReceive().UploadSourceImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSourceAsync_WithNewImage_ReplacesImageAndDeletesOldImageAfterSave()
    {
        var source = CreateSource("Loja antiga", UserId, SourceId, "sources/old.png");
        var dto = CreateUpdateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));
        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));
        _uploadService.UploadSourceImageAsync(dto.ImageUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sources/new.png"));

        await _sut.UpdateSourceAsync(dto);

        Assert.Equal("sources/new.png", source.ImageUrl);
        await _uploadService.Received(1).UploadSourceImageAsync(dto.ImageUrl, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.Received(1).DeleteImageAsync("sources/old.png", Arg.Any<CancellationToken>());
        _sourceRepository.Received(1).Update(source);
    }

    [Fact]
    public async Task UpdateSourceAsync_WhenNameAlreadyExists_ThrowsBeforeUploadingOrUpdating()
    {
        var source = CreateSource("Loja antiga", UserId, SourceId, "sources/old.png");
        var dto = CreateUpdateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));
        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource(dto.Name, UserId, SourceId + 1)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateSourceAsync(dto));

        Assert.Equal("SOURCE_NAME_ALREADY_EXISTS", exception.Code);
        await _uploadService.DidNotReceive().UploadSourceImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        _sourceRepository.DidNotReceive().Update(Arg.Any<Source>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSourceAsync_WhenSavingNewImageFails_DeletesNewImageAndRethrows()
    {
        var source = CreateSource("Loja antiga", UserId, SourceId, "sources/old.png");
        var dto = CreateUpdateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));
        _sourceRepository.GetByNameAndUserIdAsync(dto.Name, UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));
        _uploadService.UploadSourceImageAsync(dto.ImageUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("sources/new.png"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("database failure"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateSourceAsync(dto));

        Assert.Equal("database failure", exception.Message);
        await _uploadService.Received(1).DeleteImageAsync("sources/new.png", Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync("sources/old.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSourceAsync_WhenSourceDoesNotExist_ThrowsWithoutUploadingOrSaving()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));
        var dto = CreateUpdateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateSourceAsync(dto));

        Assert.Equal("SOURCE_NOT_FOUND", exception.Code);
        await _sourceRepository.DidNotReceive().GetByNameAndUserIdAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().UploadSourceImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSourceAsync_WhenSourceBelongsToAnotherUser_ThrowsWithoutUploadingOrSaving()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource("Loja alheia", OtherUserId, SourceId)));
        var dto = CreateUpdateDto();
        dto.ImageUrl = Substitute.For<IFormFile>();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateSourceAsync(dto));

        Assert.Equal("SOURCE_ACCESS_DENIED", exception.Code);
        await _uploadService.DidNotReceive().UploadSourceImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSourceAsync_WhenSourceHasImage_RemovesSavesThenDeletesImage()
    {
        var source = CreateSource("Loja", UserId, SourceId, "sources/remove.png");
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));

        await _sut.DeleteSourceAsync(SourceId);

        _sourceRepository.Received(1).Remove(source);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.Received(1).DeleteImageAsync("sources/remove.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSourceAsync_WhenSourceHasNoImage_DoesNotDeleteImage()
    {
        var source = CreateSource("Loja", UserId, SourceId);
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));

        await _sut.DeleteSourceAsync(SourceId);

        _sourceRepository.Received(1).Remove(source);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSourceAsync_WhenSourceDoesNotExist_ThrowsWithoutRemovingOrSaving()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteSourceAsync(SourceId));

        Assert.Equal("SOURCE_NOT_FOUND", exception.Code);
        _sourceRepository.DidNotReceive().Remove(Arg.Any<Source>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSourceAsync_WhenSourceBelongsToAnotherUser_ThrowsWithoutRemovingOrSaving()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource("Loja alheia", OtherUserId, SourceId, "sources/owned-by-other.png")));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteSourceAsync(SourceId));

        Assert.Equal("SOURCE_ACCESS_DENIED", exception.Code);
        _sourceRepository.DidNotReceive().Remove(Arg.Any<Source>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uploadService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListSourceByIdAsync_WhenSourceBelongsToUser_ReturnsMappedSource()
    {
        var source = CreateSource("Loja", UserId, SourceId, "sources/image.png", "https://example.com");
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));

        var result = await _sut.ListSourceByIdAsync(SourceId);

        Assert.Equal(SourceId, result.Id);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(source.Name, result.Name);
        Assert.Equal(source.ImageUrl, result.ImageUrl);
        Assert.Equal(source.Link, result.Link);
        Assert.Equal(source.CreatedAt, result.CreatedAt);
        Assert.Equal(source.IsActive, result.IsActive);
        Assert.Equal(source.UtCreation, result.UtCreation);
    }

    [Fact]
    public async Task ListSourceByIdAsync_WhenSourceDoesNotExist_ThrowsNotFound()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ListSourceByIdAsync(SourceId));

        Assert.Equal("SOURCE_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task ListSourceByIdAsync_WhenSourceBelongsToAnotherUser_ThrowsAccessDenied()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource("Loja alheia", OtherUserId, SourceId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ListSourceByIdAsync(SourceId));

        Assert.Equal("SOURCE_ACCESS_DENIED", exception.Code);
    }

    [Fact]
    public async Task ListSourcesByUserAsync_ForwardsSearchAndMapsPagedSources()
    {
        var source = CreateSource("Loja", UserId, SourceId, "sources/image.png");
        _sourceRepository.GetPageByUserIdAsync(UserId, 2, 5, "loja", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<Source> Items, int TotalCount)>(([source], 12)));

        var result = await _sut.ListSourcesByUserAsync(2, 5, "loja");

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(12, result.TotalItems);
        Assert.Equal("Loja", Assert.Single(result.Items).Name);
        await _sourceRepository.Received(1).GetPageByUserIdAsync(
            UserId, 2, 5, "loja", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListSourcesByUserAsync_NormalizesPageAndPageSize()
    {
        _sourceRepository.GetPageByUserIdAsync(UserId, 1, 50, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<Source> Items, int TotalCount)>((Array.Empty<Source>(), 0)));

        var result = await _sut.ListSourcesByUserAsync(0, 100, null);

        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
    }

    [Fact]
    public async Task GetSourceImageAsync_WhenSourceHasImage_ReturnsImageStream()
    {
        var source = CreateSource("Loja", UserId, SourceId, "sources/image.png");
        using var imageStream = new MemoryStream([1, 2, 3]);
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(source));
        _uploadService.ReadUploadAsync("sources/image.png", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(imageStream));

        var result = await _sut.GetSourceImageAsync(SourceId);

        Assert.Same(imageStream, result);
        await _uploadService.Received(1).ReadUploadAsync("sources/image.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSourceImageAsync_WhenSourceHasNoImage_ReturnsNullWithoutReadingFile()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource("Loja", UserId, SourceId)));

        var result = await _sut.GetSourceImageAsync(SourceId);

        Assert.Null(result);
        await _uploadService.DidNotReceive().ReadUploadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSourceImageAsync_WhenSourceDoesNotExist_ThrowsWithoutReadingFile()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetSourceImageAsync(SourceId));

        Assert.Equal("SOURCE_NOT_FOUND", exception.Code);
        await _uploadService.DidNotReceive().ReadUploadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSourceImageAsync_WhenSourceBelongsToAnotherUser_ThrowsWithoutReadingFile()
    {
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource("Loja alheia", OtherUserId, SourceId, "private.png")));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.GetSourceImageAsync(SourceId));

        Assert.Equal("SOURCE_ACCESS_DENIED", exception.Code);
        await _uploadService.DidNotReceive().ReadUploadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static CreateSourceDTO CreateCreateDto() => new()
    {
        Name = "Loja de teste",
        Link = "https://example.com"
    };

    private static UpdateSourceDTO CreateUpdateDto() => new()
    {
        Id = SourceId,
        Name = "Loja atualizada",
        Link = "https://updated.example.com",
        IsActive = false
    };

    private static Source CreateSource(string name, int userId, int id, string? imageUrl = null, string? link = null)
    {
        var source = Source.Create(userId, name, imageUrl, link, true, userId);
        typeof(Source).GetProperty(nameof(Source.Id))!.SetValue(source, id);
        return source;
    }
}
