namespace Application.DTOs.Transaction
{
    public class GetTransactionTypeDTO
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int? UtCreation { get; set; }
    }
}
