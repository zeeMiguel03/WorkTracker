using Application.Interfaces;
using Domain.Exceptions;
using Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Services
{
    public sealed class UploadService : IUploadService
    {
        private const string UsersFolder = "users";
        private const string OutputExtension = ".webp";

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private static readonly HashSet<string> AllowedFormats =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "JPEG",
                "PNG",
                "WEBP"
            };

        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        private static readonly Configuration ImageConfiguration =
            CreateImageConfiguration();

        private readonly string _rootUploadPath;
        private readonly UploadOptions _options;
        private readonly ILogger<UploadService> _logger;

        public UploadService(
            IWebHostEnvironment environment,
            IOptions<UploadOptions> options,
            ILogger<UploadService> logger)
        {
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _options = options.Value;
            _logger = logger;

            var webRootPath = environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            }

            _rootUploadPath = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));

            Directory.CreateDirectory(_rootUploadPath);
            RejectSymbolicLinksInPath(_rootUploadPath);
        }

        public Task<string> UploadUserImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadImageAsync(file, UsersFolder, cancellationToken);
        }

        public Task DeleteImageAsync(string? relativePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return Task.CompletedTask;
            }

            var fullPath = GetPhysicalPath(relativePath);

            if (File.Exists(fullPath))
            {
                RejectSymbolicLinksInPath(fullPath);
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        public Task<Stream> ReadUploadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new DomainException("UPLOAD_PATH_INVALID", "The file path is invalid.");
            }

            var fullPath = GetPhysicalPath(relativePath);

            if (!File.Exists(fullPath))
            {
                throw new DomainException("UPLOAD_FILE_NOT_FOUND", "The file was not found.");
            }

            RejectSymbolicLinksInPath(fullPath);

            Stream stream = new FileStream(
                fullPath,
                new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.Read,
                    BufferSize = 64 * 1024,
                    Options = FileOptions.Asynchronous |
                              FileOptions.SequentialScan
                });

            return Task.FromResult(stream);
        }

        private async Task<string> UploadImageAsync(IFormFile file, string subFolder, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(file);
            cancellationToken.ThrowIfCancellationRequested();

            ValidateFileSize(file);

            var originalExtension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            ValidateExtension(originalExtension);

            var decoderOptions = new DecoderOptions
            {
                Configuration = ImageConfiguration,
                MaxFrames = 1,
                SkipMetadata = false
            };

            ImageInfo imageInfo;

            try
            {
                await using var identifyStream = file.OpenReadStream();

                imageInfo = await Image.IdentifyAsync(decoderOptions, identifyStream, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidMemoryOperationException)
            {
                throw ProcessingLimitException();
            }
            catch (Exception exception) when (
                exception is ImageFormatException or NotSupportedException)
            {
                throw InvalidImageException(exception);
            }

            ValidateDetectedFormat(imageInfo, originalExtension);
            ValidateSourceDimensions(imageInfo);

            Image image;

            try
            {
                await using var inputStream = file.OpenReadStream();

                var loadOptions = new DecoderOptions
                {
                    Configuration = ImageConfiguration,
                    MaxFrames = 1,
                    SkipMetadata = false,

                    TargetSize = new Size(
                        _options.OutputMaxWidth,
                        _options.OutputMaxHeight)
                };

                image = await Image.LoadAsync(loadOptions, inputStream, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidMemoryOperationException)
            {
                throw ProcessingLimitException();
            }
            catch (Exception exception) when (exception is ImageFormatException or NotSupportedException)
            {
                throw InvalidImageException(exception);
            }

            try
            {
                using (image)
                {
                    image.Mutate(context => context.AutoOrient());

                    if (image.Width > _options.OutputMaxWidth ||
                        image.Height > _options.OutputMaxHeight)
                    {
                        image.Mutate(context => context.Resize(
                            new ResizeOptions
                            {
                                Size = new Size(
                                    _options.OutputMaxWidth,
                                    _options.OutputMaxHeight),

                                Mode = ResizeMode.Max
                            }));
                    }

                    return await SaveImageAsync(image, subFolder, cancellationToken);
                }
            }
            catch (InvalidMemoryOperationException)
            {
                throw ProcessingLimitException();
            }
        }

        private async Task<string> SaveImageAsync(Image image, string subFolder, CancellationToken cancellationToken)
        {
            var targetFolder = Path.Combine(_rootUploadPath, subFolder);

            EnsurePathInsideUploads(targetFolder);
            Directory.CreateDirectory(targetFolder);
            RejectSymbolicLinksInPath(targetFolder);

            var identifier = Guid.NewGuid().ToString("N");
            var fileName = $"{identifier}{OutputExtension}";
            var temporaryFileName = $".{identifier}.tmp";

            var finalPath = Path.Combine(targetFolder, fileName);
            var temporaryPath = Path.Combine(
                targetFolder,
                temporaryFileName);

            EnsurePathInsideUploads(finalPath);
            EnsurePathInsideUploads(temporaryPath);

            try
            {
                await using (var outputStream = new FileStream(
                    temporaryPath,
                    new FileStreamOptions
                    {
                        Mode = FileMode.CreateNew,
                        Access = FileAccess.Write,
                        Share = FileShare.None,
                        BufferSize = 64 * 1024,
                        Options = FileOptions.Asynchronous |
                                  FileOptions.SequentialScan
                    }))
                {
                    var encoder = new WebpEncoder
                    {
                        Quality = _options.WebpQuality,
                        SkipMetadata = true
                    };

                    await image.SaveAsync(outputStream, encoder, cancellationToken);

                    await outputStream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                File.Move(
                    temporaryPath,
                    finalPath,
                    overwrite: false);

                return $"uploads/{subFolder}/{fileName}";
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception exception) when (
                        exception is IOException or UnauthorizedAccessException)
                    {
                        _logger.LogWarning(
                            exception,
                            "Failed to delete temporary upload file {TemporaryFilePath}.",
                            temporaryPath);
                    }
                }
            }
        }

        private void ValidateFileSize(IFormFile file)
        {
            if (file.Length <= 0)
            {
                throw new DomainException("UPLOAD_FILE_REQUIRED", "No file was uploaded.");
            }

            if (file.Length > _options.MaxFileSizeBytes)
            {
                throw new DomainException("UPLOAD_SIZE_EXCEEDED", $"Maximum allowed size is " + $"{_options.MaxFileSizeBytes} bytes.");
            }
        }

        private static void ValidateExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedExtensions.Contains(extension))
            {
                throw new DomainException(
                    "UPLOAD_EXTENSION_INVALID",
                    $"Invalid extension ({extension}). " +
                    $"Allowed extensions: " +
                    $"{string.Join(", ", AllowedExtensions)}");
            }
        }

        private static void ValidateDetectedFormat(ImageInfo imageInfo, string originalExtension)
        {
            var detectedFormat = imageInfo.Metadata.DecodedImageFormat;

            if (detectedFormat is null ||
                !AllowedFormats.Contains(detectedFormat.Name))
            {
                throw new DomainException("UPLOAD_TYPE_INVALID", "The file is not a supported image.");
            }

            if (!ExtensionMatchesFormat(originalExtension, detectedFormat.Name))
            {
                throw new DomainException("UPLOAD_TYPE_MISMATCH", "The file extension does not match " + "the detected image format.");
            }
        }

        private void ValidateSourceDimensions(ImageInfo imageInfo)
        {
            var pixels = (long)imageInfo.Width * imageInfo.Height;

            if (imageInfo.Width > _options.MaxSourceWidth ||
                imageInfo.Height > _options.MaxSourceHeight ||
                pixels > _options.MaxSourcePixels)
            {
                throw new DomainException("UPLOAD_DIMENSIONS_EXCEEDED", "The image dimensions are too large.",
                    new
                    {
                        imageInfo.Width,
                        imageInfo.Height,
                        Pixels = pixels,
                        _options.MaxSourceWidth,
                        _options.MaxSourceHeight,
                        _options.MaxSourcePixels
                    });
            }
        }

        private string GetPhysicalPath(string relativePath)
        {
            try
            {
                var normalizedPath = relativePath
                    .Trim()
                    .Replace(
                        Path.AltDirectorySeparatorChar,
                        Path.DirectorySeparatorChar)
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar)
                    .Replace(
                        '\\',
                        Path.DirectorySeparatorChar);

                if (Path.IsPathRooted(normalizedPath))
                {
                    throw InvalidPathException();
                }

                var uploadsPrefix =
                    "uploads" + Path.DirectorySeparatorChar;

                if (normalizedPath.StartsWith(uploadsPrefix, PathComparison))
                {
                    normalizedPath = normalizedPath[uploadsPrefix.Length..];
                }

                var fullPath = Path.GetFullPath(Path.Combine(_rootUploadPath,normalizedPath));

                EnsurePathInsideUploads(fullPath);

                return fullPath;
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw InvalidPathException(exception);
            }
        }

        private void EnsurePathInsideUploads(string path)
        {
            var fullRootPath = Path
                .GetFullPath(_rootUploadPath)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

            var fullTargetPath = Path.GetFullPath(path);

            var rootPrefix = fullRootPath + Path.DirectorySeparatorChar;

            var isRoot = fullTargetPath.Equals(
                fullRootPath,
                PathComparison);

            var isInsideRoot = fullTargetPath.StartsWith(
                rootPrefix,
                PathComparison);

            if (!isRoot && !isInsideRoot)
            {
                throw InvalidPathException();
            }
        }

        private void RejectSymbolicLinksInPath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            EnsurePathInsideUploads(fullPath);

            RejectSymbolicLinkIfItExists(_rootUploadPath);

            var relativePath = Path.GetRelativePath(_rootUploadPath, fullPath);

            if (relativePath == ".")
            {
                return;
            }

            var currentPath = _rootUploadPath;

            foreach (var segment in relativePath.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries))
            {
                currentPath = Path.Combine(currentPath, segment);
                RejectSymbolicLinkIfItExists(currentPath);
            }
        }

        private static void RejectSymbolicLinkIfItExists(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return;
            }

            var attributes = File.GetAttributes(path);

            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw InvalidPathException();
            }
        }

        private static Configuration CreateImageConfiguration()
        {
            var configuration = Configuration.Default.Clone();

            configuration.MaxDegreeOfParallelism = 2;
            configuration.MemoryAllocator = MemoryAllocator.Create(
                new MemoryAllocatorOptions
                {
                    AllocationLimitMegabytes = 128,
                    AccumulativeAllocationLimitMegabytes = 256,
                    MaximumPoolSizeMegabytes = 64
                });

            return configuration;
        }

        private static bool ExtensionMatchesFormat(string extension, string formatName)
        {
            return formatName.ToUpperInvariant() switch
            {
                "JPEG" => extension is ".jpg" or ".jpeg",
                "PNG" => extension == ".png",
                "WEBP" => extension == ".webp",
                _ => false
            };
        }

        private static DomainException InvalidImageException(Exception? innerException = null)
        {
            return new DomainException(
                "UPLOAD_CONTENT_INVALID",
                "The uploaded file is not a valid image.",
                innerException is null
                    ? null
                    : new { ExceptionType = innerException.GetType().Name });
        }

        private static DomainException ProcessingLimitException()
        {
            return new DomainException(
                "UPLOAD_PROCESSING_LIMIT_EXCEEDED",
                "The image requires too much memory to process.");
        }

        private static DomainException InvalidPathException(Exception? innerException = null)
        {
            return new DomainException(
                "UPLOAD_PATH_INVALID",
                "The file path is invalid.",
                innerException is null
                    ? null
                    : new { ExceptionType = innerException.GetType().Name });
        }
    }
}
