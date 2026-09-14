namespace Application.DTOs.User
{
    public class ChangeUserEmailDTO
    {
        public string newEmail { get; set; } = null!;

        public string currentPassword { get; set; } = null!;
    }
}
