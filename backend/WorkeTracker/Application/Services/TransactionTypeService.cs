using Application.DTOs.Transaction;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services
{
    public class TransactionTypeService : ITransactionTypeService
    {
        private readonly ITransactionTypeRepository _transactionTypeRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionTypeService(
            ITransactionTypeRepository transactionTypeRepo,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _transactionTypeRepo = transactionTypeRepo;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<GetTransactionTypeDTO> CreateTransactionTypeAsync(CreateTransactionTypeDTO dto, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var verifyExistentTransactionType = await _transactionTypeRepo.GetByNameAsync(currentUserId, dto.Name, cancellationToken);

            if (verifyExistentTransactionType is not null)
            {
                throw new DomainException("TRANSACTION_TYPE_NAME_ALREADY_EXISTS", "A transaction type with this name already exists.");
            }

            var transactionType = TransactionType.Create(
                currentUserId,
                dto.Name,
                dto.Color,
                currentUserId);

            await _transactionTypeRepo.AddAsync(transactionType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToGetTransactionTypeDTO(transactionType);
        }

        public async Task<GetTransactionTypeDTO> ListTransactionTypeByIdAsync(int idTransactionType, CancellationToken cancellationToken = default)
        {
            var transactionType = await ValidatePermissionsAsync(idTransactionType, "You are not allowed to access this transaction type.", cancellationToken);

            return MapToGetTransactionTypeDTO(transactionType);
        }

        public async Task<List<GetTransactionTypeDTO>> ListTransactionTypesByUserAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var transactionTypes = await _transactionTypeRepo.GetByUserAsync(currentUserId, cancellationToken);

            return transactionTypes.Select(MapToGetTransactionTypeDTO).ToList();
        }

        public async Task UpdateTransactionTypeAsync(UpdateTransactionTypeDTO dto, CancellationToken cancellationToken = default)
        {
            var transactionType = await ValidatePermissionsAsync(dto.idTransactionType, "You are not allowed to modify this transaction type.", cancellationToken);

            var transactionTypeWithSameName = await _transactionTypeRepo.GetByNameAsync(transactionType.UserId, dto.Name, cancellationToken);

            if (transactionTypeWithSameName is not null && transactionTypeWithSameName.Id != transactionType.Id)
            {
                throw new DomainException("TRANSACTION_TYPE_NAME_ALREADY_EXISTS", "A transaction type with this name already exists.");
            }

            transactionType.Update(
                dto.Name,
                dto.Color);

            _transactionTypeRepo.Update(transactionType);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteTransactionTypeAsync(int idTransactionType, CancellationToken cancellationToken = default)
        {
            var transactionType = await ValidatePermissionsAsync(idTransactionType, "You are not allowed to delete this transaction type.", cancellationToken);

            if (await _transactionTypeRepo.HasAssociatedEntriesAsync(transactionType.Id, cancellationToken))
            {
                throw new DomainException("TRANSACTION_TYPE_IN_USE", "The transaction type cannot be deleted because it is associated with entries.");
            }

            _transactionTypeRepo.Remove(transactionType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<TransactionType> ValidatePermissionsAsync(int id, string exceptionText, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.GetUserId();

            var transactionType = await _transactionTypeRepo.GetByIdAsync(id, cancellationToken);

            if (transactionType is null)
            {
                throw new DomainException("TRANSACTION_TYPE_NOT_FOUND", "Transaction type was not found.");
            }

            if (transactionType.UserId != currentUserId)
            {
                throw new UnauthorizedException("TRANSACTION_TYPE_ACCESS_DENIED", exceptionText);
            }

            return transactionType;
        }

        private static GetTransactionTypeDTO MapToGetTransactionTypeDTO(TransactionType transactionType)
        {
            return new GetTransactionTypeDTO
            {
                Id = transactionType.Id,
                UserId = transactionType.UserId,
                Name = transactionType.Name,
                Color = transactionType.Color,
                CreatedAt = transactionType.CreatedAt,
                UtCreation = transactionType.UtCreation
            };
        }
    }
}
