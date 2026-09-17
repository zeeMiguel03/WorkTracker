using Application.DTOs.Account;
using Application.DTOs.Common;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public AccountService(
            IAccountRepository accountRepo,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _accountRepo = accountRepo;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ListAccountDTO> CreateAccountAsync(CreateAccountDTO accountDTO, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var account = Account.Create(
                currentUserId,
                accountDTO.AccountType,
                accountDTO.Name,
                accountDTO.BankName,
                accountDTO.CardBrand,
                accountDTO.Last4,
                accountDTO.IconKey,
                accountDTO.Color,
                accountDTO.InitialBalance,
                accountDTO.IncludeInTotal,
                currentUserId);

            await _accountRepo.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToGetAccountDTO(account);
        }

        public async Task UpdateAccountAsync(UpdateAccountDTO accountDTO, CancellationToken cancellationToken = default)
        {
            var account = await ValidatePermissionsAsync(accountDTO.IdAccount, "You are not allowed to modify this account", cancellationToken);

            account.Update(
                accountDTO.AccountType,
                accountDTO.Name,
                accountDTO.BankName,
                accountDTO.CardBrand,
                accountDTO.Last4,
                accountDTO.IconKey,
                accountDTO.Color,
                accountDTO.InitialBalance,
                accountDTO.IncludeInTotal);

            _accountRepo.Update(account);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<PagedResultDTO<ListAccountDTO>> ListAccountsByUserAsync(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var currentUser = _currentUserService.GetUserId();

            var result = await _accountRepo.GetPageByUserIdAsync(
                currentUser,
                page,
                pageSize,
                search,
                cancellationToken);

            return new PagedResultDTO<ListAccountDTO>
            {
                Items = result.Items.Select(MapToGetAccountDTO).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = result.TotalCount,
            };
        }

        public async Task<ListAccountDTO> ListAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var account = await ValidatePermissionsAsync(accountId, "You are not allowed to access this account.", cancellationToken);

            return MapToGetAccountDTO(account);
        }

        public async Task DeleteAccountAsync(int idAccount, CancellationToken cancellation = default)
        {
            var account = await ValidatePermissionsAsync(idAccount, "You are not allowed to delete this account.", cancellation);

            _accountRepo.Remove(account);

            await _unitOfWork.SaveChangesAsync(cancellation);
        }

        private async Task<Account> ValidatePermissionsAsync(int id, string exceptionText, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.GetUserId();

            var account = await _accountRepo.GetByIdAsync(id, cancellationToken);

            if (account is null)
            {
                throw new DomainException("ACCOUNT_NOT_FOUND", "Account was not found.");
            }

            if (account.UserId != currentUserId)
            {
                throw new UnauthorizedException("ACCOUNT_ACCESS_DENIED", exceptionText);
            }

            return account;
        }

        private static ListAccountDTO MapToGetAccountDTO(Account account)
        {
            return new ListAccountDTO
            {
                Id = account.Id,
                UserId = account.UserId,
                AccountType = account.AccountType,
                Name = account.Name,
                BankName = account.BankName,
                CardBrand = account.CardBrand,
                Last4 = account.Last4,
                IconKey = account.IconKey,
                Color = account.Color,
                InitialBalance = account.InitialBalance,
                IncludeInTotal = account.IncludeInTotal,
                CreatedAt = account.CreatedAt,
                UtCreation = account.UtCreation
            };
        }
    }
}
