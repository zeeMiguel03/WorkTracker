using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IUploadService
    {
        Task<string> UploadUserImageAsync(IFormFile file, CancellationToken cancellationToken = default);

        Task<string> UploadSourceImageAsync(IFormFile file, CancellationToken cancellationToken = default);

        Task DeleteImageAsync(string? relativePath, CancellationToken cancellationToken = default);

        Task<Stream> ReadUploadAsync(string relativePath, CancellationToken cancellationToken = default);
    }
}