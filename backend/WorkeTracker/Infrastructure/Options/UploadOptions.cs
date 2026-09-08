using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options
{
    public sealed class UploadOptions
    {
        public const string SectionName = "Upload";

        [Range(1, long.MaxValue)]
        public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;

        [Range(1, 50_000)]
        public int MaxSourceWidth { get; init; } = 8_192;

        [Range(1, 50_000)]
        public int MaxSourceHeight { get; init; } = 8_192;

        [Range(1, 500_000_000)]
        public long MaxSourcePixels { get; init; } = 16_000_000;

        [Range(1, 10_000)]
        public int OutputMaxWidth { get; init; } = 2_048;

        [Range(1, 10_000)]
        public int OutputMaxHeight { get; init; } = 2_048;

        [Range(1, 100)]
        public int WebpQuality { get; init; } = 85;
    }
}
