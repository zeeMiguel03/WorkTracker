using Application.DTOs.Source;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class SourceService : ISourceService
    {
        private readonly ISourceRepository _sourceRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUploadService _uploadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SourceService> _logger;

        public SourceService(
            ISourceRepository sourceRepo,
            ICurrentUserService currentUserService,
            IUploadService uploadService,
            IUnitOfWork unitOfWork,
            ILogger<SourceService> logger)
        {
            _sourceRepo = sourceRepo;
            _currentUserService = currentUserService;
            _uploadService = uploadService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GetSourceDTO> CreateSourceAsync(CreateSourceDTO dto, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            await ValidateSourceNameAvailableAsync(dto.Name, currentUserId, null, cancellationToken);

            string? sourceImagePath = null;

            try
            {
                if (dto.ImageUrl is not null)
                {
                    sourceImagePath = await _uploadService.UploadSourceImageAsync(dto.ImageUrl, cancellationToken);
                }

                var source = Source.Create(
                    currentUserId,
                    dto.Name,
                    sourceImagePath,
                    true,
                    currentUserId);

                await _sourceRepo.AddAsync(source, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return MapToGetSourceDTO(source);
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(sourceImagePath))
                {
                    await TryDeleteSourceImageAsync(sourceImagePath);
                }

                throw;
            }
        }

        public async Task UpdateSourceAsync(UpdateSourceDTO dto, CancellationToken cancellationToken = default)
        {
            var source = await GetOwnedSourceAsync(
                dto.Id,
                "You do not have permission to update this source.",
                cancellationToken);

            await ValidateSourceNameAvailableAsync(dto.Name, source.UserId, source.Id, cancellationToken);

            var oldImagePath = source.ImageUrl;
            string? newImagePath = null;

            try
            {
                if (dto.ImageUrl is not null)
                {
                    newImagePath = await _uploadService.UploadSourceImageAsync(dto.ImageUrl, cancellationToken);
                }

                var sourceImagePath = newImagePath ?? oldImagePath;

                source.Update(dto.Name, sourceImagePath, dto.IsActive);
                _sourceRepo.Update(source);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(newImagePath) && !string.IsNullOrWhiteSpace(oldImagePath))
                {
                    await TryDeleteSourceImageAsync(oldImagePath);
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newImagePath))
                {
                    await TryDeleteSourceImageAsync(newImagePath);
                }

                throw;
            }
        }

        public async Task DeleteSourceAsync(int idSource, CancellationToken cancellationToken = default)
        {
            var source = await GetOwnedSourceAsync(
                idSource,
                "You do not have permission to delete this source.",
                cancellationToken);

            var sourceImagePath = source.ImageUrl;

            _sourceRepo.Remove(source);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(sourceImagePath))
            {
                await TryDeleteSourceImageAsync(sourceImagePath);
            }
        }

        public async Task<GetSourceDTO> ListSourceByIdAsync(int idSource, CancellationToken cancellationToken = default)
        {
            var source = await GetOwnedSourceAsync(
                idSource,
                "You do not have permission to access this source.",
                cancellationToken);

            return MapToGetSourceDTO(source);
        }

        public async Task<List<GetSourceDTO>> ListSourcesByUserAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();
            var sources = await _sourceRepo.GetByUserIdAsync(currentUserId, cancellationToken);

            return sources
                .Select(MapToGetSourceDTO)
                .ToList();
        }

        public async Task<Stream?> GetSourceImageAsync(int idSource, CancellationToken cancellationToken = default)
        {
            var source = await GetOwnedSourceAsync(
                idSource,
                "You do not have permission to access this source image.",
                cancellationToken);

            return string.IsNullOrWhiteSpace(source.ImageUrl)
                ? null
                : await _uploadService.ReadUploadAsync(source.ImageUrl, cancellationToken);
        }

        private async Task<Source> GetOwnedSourceAsync(
            int sourceId,
            string accessDeniedMessage,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.GetUserId();
            var source = await _sourceRepo.GetByIdAsync(sourceId, cancellationToken);

            if (source is null)
            {
                throw new DomainException("SOURCE_NOT_FOUND", "Source was not found.");
            }

            if (source.UserId != currentUserId)
            {
                throw new DomainException("SOURCE_ACCESS_DENIED", accessDeniedMessage);
            }

            return source;
        }

        private async Task ValidateSourceNameAvailableAsync(
            string name,
            int userId,
            int? sourceId,
            CancellationToken cancellationToken)
        {
            var sourceWithSameName = await _sourceRepo.GetByNameAndUserIdAsync(name, userId, cancellationToken);

            if (sourceWithSameName is not null && (sourceId is null || sourceWithSameName.Id != sourceId.Value))
            {
                throw new DomainException("SOURCE_NAME_ALREADY_EXISTS", "A source with this name already exists.");
            }
        }

        private async Task TryDeleteSourceImageAsync(string sourceImagePath)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                await _uploadService.DeleteImageAsync(sourceImagePath, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete source image {SourceImagePath}.",
                    sourceImagePath);
            }
        }

        private static GetSourceDTO MapToGetSourceDTO(Source source)
        {
            return new GetSourceDTO
            {
                Id = source.Id,
                UserId = source.UserId,
                Name = source.Name,
                ImageUrl = source.ImageUrl,
                CreatedAt = source.CreatedAt,
                IsActive = source.IsActive,
                UtCreation = source.UtCreation
            };
        }
    }
}
