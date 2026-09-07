namespace Application.DTOs.User
{
    public class ChangeUserPasswordDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}
