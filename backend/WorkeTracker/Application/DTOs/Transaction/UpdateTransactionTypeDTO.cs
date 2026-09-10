namespace Application.DTOs.Transaction
{
    public class UpdateTransactionTypeDTO
    {
        public int idTransactionType { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;
    }
}
