namespace Application.DTOs.Entry
{
    public class GetEntryDTO
    {
        public int Id { get; set; }

        public int SourceId { get; set; }

        public int TransactionTypeId { get; set; }

        public int AccountId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public decimal Quantity { get; set; }

        public decimal Value { get; set; }

        public DateTime Date { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? UtCreation { get; set; }
    }
}
