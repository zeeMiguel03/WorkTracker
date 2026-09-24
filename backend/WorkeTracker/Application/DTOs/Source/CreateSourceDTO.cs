using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Source
{
    public class CreateSourceDTO
    {
        public string Name { get; set; } = string.Empty;

        public IFormFile? ImageUrl { get; set; }

        public string? Link { get; set; }
    }
}
