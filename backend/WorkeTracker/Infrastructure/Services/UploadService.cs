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
    /// <summary>
    /// Provides secure storage, processing, reading, and deletion
    /// of uploaded images.
    /// </summary>
    /// <remarks>
    /// Uploaded images are validated using their actual encoded format,
    /// resized when necessary, stripped of metadata, and normalized to WebP.
    ///
    /// The original uploaded file is never stored directly.
    /// </remarks>
    public sealed class UploadService : IUploadService
    {
        /// <summary>
        /// Directory used to store user profile images inside the uploads root.
        /// </summary>
        private const string UsersFolder = "users";

        /// <summary>
        /// Directory used to store source images inside the uploads root.
        /// </summary>
        private const string SourceFolder = "sources";

        /// <summary>
        /// Directory used to store entry images inside the uploads root.
        /// </summary>
        private const string EntryFolder = "entries";

        /// <summary>
        /// Directory used to store products images inside the uploads root.
        /// </summary>
        private const string ProductsFolder = "products";

        /// <summary>
        /// Extension used for every processed image.
        /// </summary>
        private const string OutputExtension = ".webp";

        /// <summary>
        /// File extensions accepted at the upload boundary.
        /// </summary>
        /// <remarks>
        /// Extensions are only an initial validation mechanism.
        /// The actual encoded image format is validated separately.
        /// </remarks>
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        /// <summary>
        /// Actual image formats accepted after content inspection.
        /// </summary>
        private static readonly HashSet<string> AllowedFormats =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "JPEG",
                "PNG",
                "WEBP"
            };

        /// <summary>
        /// Defines the appropriate path comparison behavior for the
        /// operating system.
        /// </summary>
        /// <remarks>
        /// Windows paths are normally case-insensitive, while Linux and
        /// other Unix-like systems are normally case-sensitive.
        /// </remarks>
        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;


        /// <summary>
        /// Shared ImageSharp configuration used by all service instances.
        /// </summary>
        /// <remarks>
        /// Sharing the configuration prevents the creation of a separate
        /// memory allocator for every request.
        /// </remarks>
        private static readonly Configuration ImageConfiguration = CreateImageConfiguration();

        private readonly string _rootUploadPath;
        private readonly UploadOptions _options;
        private readonly ILogger<UploadService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadService"/> class.
        /// </summary>
        /// <param name="environment">
        /// Provides the application content root and web root paths.
        /// </param>
        /// <param name="options">
        /// Contains upload size, dimension, and output quality settings.
        /// </param>
        /// <param name="logger">
        /// Logger used to record infrastructure cleanup failures.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required dependency is null.
        /// </exception>
        /// <exception cref="DomainException">
        /// Thrown when the upload root contains a symbolic link or junction.
        /// </exception>
        public UploadService(IWebHostEnvironment environment, IOptions<UploadOptions> options, ILogger<UploadService> logger)
        {
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _options = options.Value;
            _logger = logger;

            var webRootPath = environment.WebRootPath;

            // WebRootPath can be null when wwwroot has not been initialized.
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            }

            _rootUploadPath = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));

            Directory.CreateDirectory(_rootUploadPath);


            // Prevent the configured upload root from redirecting file
            // operations through a symbolic link or filesystem junction.
            RejectSymbolicLinksInPath(_rootUploadPath);
        }

        /// <summary>
        /// Validates, processes, and stores a user profile image.
        /// </summary>
        /// <param name="file">Image received from the HTTP request.</param>
        /// <param name="cancellationToken">
        /// Token used to cancel the upload and image-processing operation.
        /// </param>
        /// <returns>
        /// A relative path such as
        /// <c>uploads/users/identifier.webp</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="file"/> is null.
        /// </exception>
        /// <exception cref="DomainException">
        /// Thrown when the file is empty, too large, has an unsupported
        /// extension or format, contains invalid image data, exceeds the
        /// dimension limits, or requires too much processing memory.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled.
        /// </exception>
        public Task<string> UploadUserImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadImageAsync(file, UsersFolder, cancellationToken);
        }

        /// <summary>
        /// Validates, processes, and stores a source image.
        /// </summary>
        /// <param name="file">Image received from the HTTP request.</param>
        /// <param name="cancellationToken">
        /// Token used to cancel the upload and image-processing operation.
        /// </param>
        /// <returns>
        /// A relative path such as
        /// <c>uploads/sources/identifier.webp</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="file"/> is null.
        /// </exception>
        /// <exception cref="DomainException">
        /// Thrown when the file is empty, too large, has an unsupported
        /// extension or format, contains invalid image data, exceeds the
        /// dimension limits, or requires too much processing memory.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled.
        /// </exception>
        public Task<string> UploadSourceImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadImageAsync(file, SourceFolder, cancellationToken);
        }

        /// <summary>
        /// Validates, processes, and stores an entry image.
        /// </summary>
        /// <param name="file">Image received from the HTTP request.</param>
        /// <param name="cancellationToken">
        /// Token used to cancel the upload and image-processing operation.
        /// </param>
        /// <returns>
        /// A relative path such as
        /// <c>uploads/entries/identifier.webp</c>.
        /// </returns>
        public Task<string> UploadEntryImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadImageAsync(file, EntryFolder, cancellationToken);
        }

        public Task<string> UploadProductImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadImageAsync(file, ProductsFolder, cancellationToken);
        }

        /// <summary>
        /// Deletes a previously stored image.
        /// </summary>
        /// <param name="relativePath">
        /// Relative path previously returned by the upload service.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the operation before deletion starts.
        /// </param>
        /// <returns>A completed task.</returns>
        /// <remarks>
        /// A null, empty, or missing file is treated as a successful no-op.
        /// </remarks>
        /// <exception cref="DomainException">
        /// Thrown when the path escapes the uploads directory or contains
        /// a symbolic link.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled.
        /// </exception>
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

        /// <summary>
        /// Opens a stored upload for asynchronous sequential reading.
        /// </summary>
        /// <param name="relativePath">
        /// Relative path previously returned by the upload service.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the operation before the stream is opened.
        /// </param>
        /// <returns>
        /// A readable file stream.
        /// </returns>
        /// <remarks>
        /// The caller owns the returned stream and must dispose it.
        /// </remarks>
        /// <exception cref="DomainException">
        /// Thrown when the path is invalid, the file does not exist,
        /// or the path contains a symbolic link.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled.
        /// </exception>
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

        /// <summary>
        /// Executes the complete validation and image-processing pipeline.
        /// </summary>
        /// <param name="file">Uploaded image.</param>
        /// <param name="subFolder">Destination folder inside uploads.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The relative path of the processed image.</returns>
        private async Task<string> UploadImageAsync(IFormFile file, string subFolder, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(file);

            cancellationToken.ThrowIfCancellationRequested();

            // Reject empty files and files exceeding the configured byte limit.
            ValidateFileSize(file);

            var originalExtension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            ValidateExtension(originalExtension);

            var decoderOptions = new DecoderOptions
            {
                Configuration = ImageConfiguration,

                // Animated images are treated as static profile images.
                MaxFrames = 1,

                // Metadata must initially be available so that EXIF
                // orientation can be applied later.
                SkipMetadata = false
            };

            ImageInfo imageInfo;

            try
            {
                await using var identifyStream = file.OpenReadStream();

                // Identify reads the encoded format and dimensions without
                // performing the complete pixel-processing pipeline.
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

            // The real encoded format must match the filename extension.
            ValidateDetectedFormat(imageInfo, originalExtension);

            // Dimension checks are performed before complete decoding.
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

                    // Supported decoders can reduce memory and CPU usage
                    // by decoding toward this target size.
                    TargetSize = new Size(
                        _options.OutputMaxWidth,
                        _options.OutputMaxHeight)
                };

                // Fully decode the image. A file with a valid header but
                // corrupted image data will fail at this stage.
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
                    // Apply EXIF orientation before metadata is removed.
                    image.Mutate(context => context.AutoOrient());


                    // Resize only when the decoded image exceeds the
                    // configured output dimensions.
                    if (image.Width > _options.OutputMaxWidth ||
                        image.Height > _options.OutputMaxHeight)
                    {
                        image.Mutate(context => context.Resize(
                            new ResizeOptions
                            {
                                Size = new Size(
                                    _options.OutputMaxWidth,
                                    _options.OutputMaxHeight),


                                // Preserve the complete image and its
                                // original aspect ratio.
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

        /// <summary>
        /// Encodes the processed image as WebP and safely stores it.
        /// </summary>
        /// <param name="image">Decoded and processed image.</param>
        /// <param name="subFolder">Destination folder inside uploads.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The relative path of the final WebP file.</returns>
        /// <remarks>
        /// The image is first written to a temporary file. After encoding
        /// completes successfully, the temporary file is moved to its final
        /// name. This prevents incomplete files from being exposed.
        /// </remarks>
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


                        // Remove EXIF, GPS, XMP, IPTC, and other metadata
                        // supported by the encoder.
                        SkipMetadata = true
                    };

                    await image.SaveAsync(outputStream, encoder, cancellationToken);

                    await outputStream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // CreateNew and overwrite:false prevent an existing file
                // from being replaced, even in the unlikely event of a
                // generated identifier collision.
                File.Move(
                    temporaryPath,
                    finalPath,
                    overwrite: false);

                // Always return a platform-independent relative path.
                return $"uploads/{subFolder}/{fileName}";
            }
            finally
            {
                // Remove partial temporary files after an error or
                // cancellation.
                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception exception) when (
                        exception is IOException or UnauthorizedAccessException)
                    {
                        // Cleanup failures must not replace the original
                        // upload or processing exception.
                        _logger.LogWarning(exception, "Failed to delete temporary upload file {TemporaryFilePath}.", temporaryPath);
                    }
                }
            }
        }

        /// <summary>
        /// Validates the uploaded file length.
        /// </summary>
        /// <param name="file">Uploaded file.</param>
        /// <exception cref="DomainException">
        /// Thrown when the file is empty or exceeds the configured limit.
        /// </exception>
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

        /// <summary>
        /// Validates the filename extension against the upload policy.
        /// </summary>
        /// <param name="extension">
        /// Lowercase filename extension, including the leading period.
        /// </param>
        /// <exception cref="DomainException">
        /// Thrown when the extension is missing or unsupported.
        /// </exception>
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

        /// <summary>
        /// Validates the actual encoded image format and confirms that it
        /// matches the filename extension.
        /// </summary>
        /// <param name="imageInfo">
        /// Image information detected by ImageSharp.
        /// </param>
        /// <param name="originalExtension">
        /// Extension extracted from the uploaded filename.
        /// </param>
        /// <exception cref="DomainException">
        /// Thrown when the encoded format is unsupported or does not
        /// match the filename extension.
        /// </exception>
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

        /// <summary>
        /// Validates source width, height, and total pixel count.
        /// </summary>
        /// <param name="imageInfo">
        /// Image information detected before complete decoding.
        /// </param>
        /// <exception cref="DomainException">
        /// Thrown when any configured dimension limit is exceeded.
        /// </exception>
        private void ValidateSourceDimensions(ImageInfo imageInfo)
        {
            // Cast before multiplication to prevent Int32 overflow.
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

        /// <summary>
        /// Converts a stored relative path into a validated physical path.
        /// </summary>
        /// <param name="relativePath">
        /// Relative path within the uploads root.
        /// </param>
        /// <returns>A normalized absolute filesystem path.</returns>
        /// <exception cref="DomainException">
        /// Thrown when the path is absolute, malformed, too long,
        /// unsupported, or escapes the uploads directory.
        /// </exception>
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

                // Never accept absolute paths from callers.
                if (Path.IsPathRooted(normalizedPath))
                {
                    throw InvalidPathException();
                }

                var uploadsPrefix =
                    "uploads" + Path.DirectorySeparatorChar;

                // Accept paths both with and without the "uploads/"
                // prefix while resolving them against the same root.
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


        /// <summary>
        /// Ensures that a path is equal to or located below the configured
        /// uploads root.
        /// </summary>
        /// <param name="path">Physical path to validate.</param>
        /// <exception cref="DomainException">
        /// Thrown when the path escapes the uploads root.
        /// </exception>
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

        /// <summary>
        /// Checks every existing path component below the uploads root
        /// for symbolic links and filesystem junctions.
        /// </summary>
        /// <param name="path">Path whose components will be checked.</param>
        /// <exception cref="DomainException">
        /// Thrown when a symbolic link or junction is detected.
        /// </exception>
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

        /// <summary>
        /// Rejects an existing file or directory when it is implemented
        /// as a symbolic link, junction, or another reparse point.
        /// </summary>
        /// <param name="path">Existing or potential filesystem path.</param>
        /// <exception cref="DomainException">
        /// Thrown when an existing path is a reparse point.
        /// </exception>
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

        /// <summary>
        /// Creates the shared ImageSharp processing configuration.
        /// </summary>
        /// <returns>A restricted ImageSharp configuration.</returns>
        /// <remarks>
        /// These values limit processing memory, not the amount of disk
        /// space used by the uploads directory.
        ///
        /// AllocationLimitMegabytes limits a single allocation.
        /// AccumulativeAllocationLimitMegabytes limits all active
        /// allocations using this allocator.
        /// MaximumPoolSizeMegabytes limits retained pooled memory.
        /// </remarks>
        private static Configuration CreateImageConfiguration()
        {
            var configuration = Configuration.Default.Clone();

            // Limit internal CPU parallelism for each processing operation.
            configuration.MaxDegreeOfParallelism = 2;

            configuration.MemoryAllocator = MemoryAllocator.Create(
                new MemoryAllocatorOptions
                {
                    // Maximum size of an individual allocation.
                    AllocationLimitMegabytes = 128,

                    // Maximum combined size of active allocations.
                    AccumulativeAllocationLimitMegabytes = 256,

                    // Maximum memory retained by the internal pool.
                    MaximumPoolSizeMegabytes = 64
                });

            return configuration;
        }

        /// <summary>
        /// Determines whether a filename extension matches the actual
        /// encoded image format.
        /// </summary>
        /// <param name="extension">Filename extension.</param>
        /// <param name="formatName">Format detected by ImageSharp.</param>
        /// <returns>
        /// <see langword="true"/> when the extension matches the format;
        /// otherwise, <see langword="false"/>.
        /// </returns>
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
        /// <summary>
        /// Creates a domain exception for invalid or corrupted image data.
        /// </summary>
        /// <param name="innerException">
        /// Optional technical exception used to identify the failure type.
        /// </param>
        /// <returns>A standardized domain exception.</returns>
        private static DomainException InvalidImageException(Exception? innerException = null)
        {
            return new DomainException(
                "UPLOAD_CONTENT_INVALID",
                "The uploaded file is not a valid image.",
                innerException is null
                    ? null
                    : new { ExceptionType = innerException.GetType().Name });
        }

        /// <summary>
        /// Creates a domain exception for an image that exceeds the
        /// configured processing-memory limits.
        /// </summary>
        /// <returns>A standardized domain exception.</returns>
        private static DomainException ProcessingLimitException()
        {
            return new DomainException(
                "UPLOAD_PROCESSING_LIMIT_EXCEEDED",
                "The image requires too much memory to process.");
        }

        /// <summary>
        /// Creates a domain exception for an unsafe or malformed path.
        /// </summary>
        /// <param name="innerException">
        /// Optional technical exception used to identify the failure type.
        /// </param>
        /// <returns>A standardized domain exception.</returns>
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
