using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Source
{
    public class UpdateSourceDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public IFormFile? ImageUrl { get; set; }

        public bool IsActive { get; set; }
    }
}
