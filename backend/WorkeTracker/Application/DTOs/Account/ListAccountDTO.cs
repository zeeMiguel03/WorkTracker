using Domain.Enums;

namespace Application.DTOs.Account
{
    public class ListAccountDTO
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public AccountType AccountType { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? BankName { get; set; }

        public string? CardBrand { get; set; }

        public string? Last4 { get; set; }

        public string? IconKey { get; set; }

        public string? Color { get; set; }

        public decimal InitialBalance { get; set; }

        public bool IncludeInTotal { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? UtCreation { get; set; }
    }
}
