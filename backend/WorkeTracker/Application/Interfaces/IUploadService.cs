using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IUploadService
    {
        Task<string> UploadUserImageAsync(IFormFile file);

        Task DeleteImageAsync(string? relativePath);

        Task<byte[]> ReadUploadAsync(string relativePath);
    }
}
