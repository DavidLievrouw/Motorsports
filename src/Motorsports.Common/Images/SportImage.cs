namespace Motorsports.Common.Images;

public static class SportImage
{
    public const int MaxBytes = 2 * 1024 * 1024;

    private static ReadOnlySpan<byte> PngSignature => [137, 80, 78, 71, 13, 10, 26, 10];
    private static ReadOnlySpan<byte> JpegSignature => [255, 216, 255];

    public static string? ContentType(ReadOnlySpan<byte> image)
    {
        if (image.StartsWith(PngSignature))
        {
            return "image/png";
        }

        if (image.StartsWith(JpegSignature))
        {
            return "image/jpeg";
        }

        if (image.Length >= 12 && image[..4].SequenceEqual("RIFF"u8) && image.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }

    public static string Url(string sportName) => "/sports/image/" + Uri.EscapeDataString(sportName);

    public static bool IsValid(byte[]? image) =>
        image is { Length: > 0 and <= MaxBytes } && ContentType(image) is not null;
}
