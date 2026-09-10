using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Entry
{
    public class CreateEntryDTO
    {
        public int SourceId { get; set; }

        public int TransactionTypeId { get; set; }

        public int AccountId { get; set; }

        public string Name { get; set; } = string.Empty;

        public IFormFile? ImageUrl { get; set; }

        public string? Description { get; set; }

        public decimal Quantity { get; set; }

        public decimal Value { get; set; }

        public DateTime Date { get; set; }
    }
}
