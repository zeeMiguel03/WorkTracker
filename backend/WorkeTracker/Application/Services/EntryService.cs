using Application.DTOs.Entry;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class EntryService : IEntryService
    {
        private readonly IEntryRepository _entryRepo;
        private readonly ISourceRepository _sourceRepo;
        private readonly ITransactionTypeRepository _transactionTypeRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUploadService _uploadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EntryService> _logger;

        public EntryService(
            IEntryRepository entryRepo,
            ISourceRepository sourceRepo,
            ITransactionTypeRepository transactionTypeRepo,
            IAccountRepository accountRepo,
            ICurrentUserService currentUserService,
            IUploadService uploadService,
            IUnitOfWork unitOfWork,
            ILogger<EntryService> logger)
        {
            _entryRepo = entryRepo;
            _sourceRepo = sourceRepo;
            _transactionTypeRepo = transactionTypeRepo;
            _accountRepo = accountRepo;
            _currentUserService = currentUserService;
            _uploadService = uploadService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GetEntryDTO> CreateEntryAsync(CreateEntryDTO dto, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            await ValidateRelationsAsync(
                dto.SourceId,
                dto.TransactionTypeId,
                dto.AccountId,
                currentUserId,
                cancellationToken);

            string? entryImagePath = null;

            try
            {
                if (dto.ImageUrl is not null)
                {
                    entryImagePath = await _uploadService.UploadEntryImageAsync(dto.ImageUrl, cancellationToken);
                }

                var entry = Entry.Create(
                    currentUserId,
                    dto.SourceId,
                    dto.TransactionTypeId,
                    dto.AccountId,
                    dto.Name,
                    entryImagePath,
                    dto.Description,
                    dto.Quantity,
                    dto.Value,
                    dto.Date,
                    currentUserId);

                await _entryRepo.AddAsync(entry, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return MapToGetEntryDTO(entry);
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(entryImagePath))
                {
                    await TryDeleteEntryImageAsync(entryImagePath);
                }

                throw;
            }
        }

        public async Task UpdateEntryAsync(UpdateEntryDTO dto, CancellationToken cancellationToken = default)
        {
            var entry = await GetOwnedEntryAsync(dto.Id, "You do not have permission to update this entry.", cancellationToken);

            var currentUserId = _currentUserService.GetUserId();

            await ValidateRelationsAsync(
                dto.SourceId,
                dto.TransactionTypeId,
                dto.AccountId,
                currentUserId,
                cancellationToken);

            var oldImagePath = entry.ImageUrl;

            string? newImagePath = null;

            try
            {
                if (dto.ImageUrl is not null)
                {
                    newImagePath = await _uploadService.UploadEntryImageAsync(dto.ImageUrl, cancellationToken);
                }

                var entryImagePath = newImagePath ?? oldImagePath;

                entry.Update(
                    dto.SourceId,
                    dto.TransactionTypeId,
                    dto.AccountId,
                    dto.Name,
                    entryImagePath,
                    dto.Description,
                    dto.Quantity,
                    dto.Value,
                    dto.Date);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(newImagePath) && !string.IsNullOrWhiteSpace(oldImagePath))
                {
                    await TryDeleteEntryImageAsync(oldImagePath);
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newImagePath))
                {
                    await TryDeleteEntryImageAsync(newImagePath);
                }

                throw;
            }
        }

        public async Task DeleteEntryAsync(int idEntry, CancellationToken cancellationToken = default)
        {
            var entry = await GetOwnedEntryAsync(idEntry, "You do not have permission to delete this entry.", cancellationToken);

            var entryImagePath = entry.ImageUrl;

            _entryRepo.Remove(entry);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(entryImagePath))
            {
                await TryDeleteEntryImageAsync(entryImagePath);
            }
        }

        public async Task<GetEntryDTO> ListEntryByIdAsync(int idEntry, CancellationToken cancellationToken = default)
        {
            var entry = await GetOwnedEntryAsync(idEntry, "You do not have permission to access this entry.", cancellationToken);

            return MapToGetEntryDTO(entry);
        }

        public async Task<List<GetEntryDTO>> ListEntriesByUserAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();
            var entries = await _entryRepo.GetByUserIdAsync(currentUserId, cancellationToken);

            return entries
                .Select(MapToGetEntryDTO)
                .ToList();
        }

        public async Task<Stream?> GetEntryImageAsync(int idEntry, CancellationToken cancellationToken = default)
        {
            var entry = await GetOwnedEntryAsync(
                idEntry,
                "You do not have permission to access this entry image.",
                cancellationToken);

            return string.IsNullOrWhiteSpace(entry.ImageUrl)
                ? null
                : await _uploadService.ReadUploadAsync(entry.ImageUrl, cancellationToken);
        }

        private async Task<Entry> GetOwnedEntryAsync(int entryId, string accessDeniedMessage, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.GetUserId();
            var entry = await _entryRepo.GetByIdAsync(entryId, cancellationToken);

            if (entry is null)
            {
                throw new DomainException("ENTRY_NOT_FOUND", "Entry was not found.");
            }

            if (entry.UserId != currentUserId)
            {
                throw new DomainException("ENTRY_ACCESS_DENIED", accessDeniedMessage);
            }

            return entry;
        }

        private async Task ValidateRelationsAsync(int sourceId, int transactionTypeId, int accountId, int userId, CancellationToken cancellationToken)
        {
            var source = await _sourceRepo.GetByIdAsync(sourceId, cancellationToken);
            var transactionType = await _transactionTypeRepo.GetByIdAsync(transactionTypeId, cancellationToken);
            var account = await _accountRepo.GetByIdAsync(accountId, cancellationToken);

            if (source is null)
            {
                throw new DomainException("SOURCE_NOT_FOUND", "Source was not found.");
            }

            if (source.UserId != userId)
            {
                throw new DomainException("SOURCE_ACCESS_DENIED", "You do not have permission to use this source.");
            }

            if (transactionType is null)
            {
                throw new DomainException("TRANSACTION_TYPE_NOT_FOUND", "Transaction type was not found.");
            }

            if (transactionType.UserId != userId)
            {
                throw new DomainException("TRANSACTION_TYPE_ACCESS_DENIED", "You do not have permission to use this transaction type.");
            }

            if (account is null)
            {
                throw new DomainException("ACCOUNT_NOT_FOUND", "Account was not found.");
            }

            if (account.UserId != userId)
            {
                throw new DomainException("ACCOUNT_ACCESS_DENIED", "You do not have permission to use this account.");
            }
        }

        private async Task TryDeleteEntryImageAsync(string entryImagePath)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                await _uploadService.DeleteImageAsync(entryImagePath, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete entry image {EntryImagePath}.",
                    entryImagePath);
            }
        }

        private static GetEntryDTO MapToGetEntryDTO(Entry entry)
        {
            return new GetEntryDTO
            {
                Id = entry.Id,
                SourceId = entry.SourceId,
                TransactionTypeId = entry.TransactionTypeId,
                AccountId = entry.AccountId,
                Name = entry.Name,
                ImageUrl = entry.ImageUrl,
                Description = entry.Description,
                Quantity = entry.Quantity,
                Value = entry.Value,
                Date = entry.Date,
                CreatedAt = entry.CreatedAt,
                UtCreation = entry.UtCreation
            };
        }
    }
}
