using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services
{
    public class UploadService : IUploadService
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024;

        private static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly string[] ImageContentTypes =
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private readonly string _rootUploadPath;

        public UploadService(IWebHostEnvironment env)
        {
            var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

            _rootUploadPath = Path.Combine(webRootPath, "uploads");

            Directory.CreateDirectory(_rootUploadPath);
        }

        public Task<string> UploadUserImageAsync(IFormFile file)
        {
            return UploadImageAsync(file, "users");
        }

        public async Task DeleteImageAsync(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var fullPath = GetPhysicalPath(relativePath);

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
            }
        }

        public async Task<byte[]> ReadUploadAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new DomainException("UPLOAD_PATH_INVALID", "The file path is invalid.");
            }

            var fullPath = GetPhysicalPath(relativePath);

            if (!File.Exists(fullPath))
            {
                throw new DomainException("UPLOAD_FILE_NOT_FOUND", "The file was not found.");
            }

            return await File.ReadAllBytesAsync(fullPath);
        }

        private async Task<string> UploadImageAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
            {
                throw new DomainException("UPLOAD_FILE_REQUIRED", "No file was uploaded.");
            }

            if (file.Length > MAX_FILE_SIZE)
            {
                throw new DomainException("UPLOAD_SIZE_EXCEEDED", "Maximum allowed size is 5MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) || !ImageExtensions.Contains(extension))
            {
                throw new DomainException(
                    "UPLOAD_EXTENSION_INVALID",
                    $"Invalid extension ({extension}). Allowed extensions: {string.Join(", ", ImageExtensions)}");
            }

            var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(contentType) || !ImageContentTypes.Contains(contentType))
            {
                throw new DomainException("UPLOAD_TYPE_INVALID", "Invalid file type.");
            }

            if (!ContentTypeMatchesExtension(extension, contentType))
            {
                throw new DomainException(
                    "UPLOAD_TYPE_MISMATCH",
                    "The file extension does not match its content type.");
            }

            await ValidateImageSignatureAsync(file, extension);

            var targetFolder = Path.Combine(_rootUploadPath, subFolder);

            EnsurePathInsideUploads(targetFolder);

            Directory.CreateDirectory(targetFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(targetFolder, fileName);

            EnsurePathInsideUploads(filePath);

            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            return $"uploads/{subFolder}/{fileName}";
        }

        private string GetPhysicalPath(string relativePath)
        {
            var normalizedPath = relativePath
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString());

            var uploadsPrefix = "uploads" + Path.DirectorySeparatorChar;

            if (normalizedPath.StartsWith(uploadsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath[uploadsPrefix.Length..];
            }

            var fullPath = Path.Combine(_rootUploadPath, normalizedPath);

            EnsurePathInsideUploads(fullPath);

            return fullPath;
        }

        private void EnsurePathInsideUploads(string path)
        {
            var fullRootPath = Path.GetFullPath(_rootUploadPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var fullTargetPath = Path.GetFullPath(path);
            var rootPrefix = fullRootPath + Path.DirectorySeparatorChar;

            if (!fullTargetPath.Equals(fullRootPath, StringComparison.OrdinalIgnoreCase) &&
                !fullTargetPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid file path.");
            }
        }

        private static async Task ValidateImageSignatureAsync(IFormFile file, string extension)
        {
            var header = new byte[12];

            await using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));

            var isValid = extension switch
            {
                ".jpg" or ".jpeg" =>
                    bytesRead >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                ".png" =>
                    bytesRead >= 8 &&
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A,

                ".webp" =>
                    bytesRead >= 12 &&
                    header[0] == 0x52 &&
                    header[1] == 0x49 &&
                    header[2] == 0x46 &&
                    header[3] == 0x46 &&
                    header[8] == 0x57 &&
                    header[9] == 0x45 &&
                    header[10] == 0x42 &&
                    header[11] == 0x50,

                _ => false
            };

            if (!isValid)
            {
                throw new DomainException("UPLOAD_CONTENT_INVALID", "The file content does not match a valid image.");
            }
        }

        private static bool ContentTypeMatchesExtension(string extension, string contentType)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => contentType == "image/jpeg",
                ".png" => contentType == "image/png",
                ".webp" => contentType == "image/webp",
                _ => false
            };
        }
    }
}