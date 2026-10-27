using System.Security.Cryptography;
using System.Text;

namespace Motorsports.Common;

/// <summary>
/// Optimistic concurrency token without a schema change: a hash of the column values as loaded.
/// Values are compared ordinally, so case-only changes are detected too.
/// </summary>
public static class RowVersion
{
    public static string Compute(params ReadOnlySpan<string?> values)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (var value in values)
        {
            if (value is null)
            {
                hash.AppendData([0]);
                continue;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
            hash.AppendData([1]);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return Convert.ToHexString(hash.GetHashAndReset().AsSpan(0, 16));
    }

    public static bool Matches(string? token, string current) =>
        token is not null && string.Equals(token, current, StringComparison.Ordinal);
}
