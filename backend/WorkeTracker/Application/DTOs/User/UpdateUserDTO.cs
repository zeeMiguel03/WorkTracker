using Microsoft.AspNetCore.Http;

namespace Application.DTOs.User
{
    public class UpdateUserDTO
    {
        public string Name { get; set; } = string.Empty;

        public IFormFile? ProfileImageUrl { get; set; }
    }
}
