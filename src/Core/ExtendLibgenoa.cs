using Serilog;

namespace MCEPatcher.Core;

public static class ExtendLibgenoa
{
    public const string Name = "extend-libgenoa";

    public const int ExtendedPadding = 10965232;
    public const long ExtendedHeaderOffset = 0x60;
    public const long ExtendedBaseAddress = 0x07000000;

    public static readonly byte[] HeaderPatch = [
        0x00, 0x20, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x20, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00
    ];

    public static void Extent(DirectoryInfo decodedFiles)
    {
        string path = Path.Combine(decodedFiles.FullName, "lib", "arm64-v8a", "libgenoa.so");

        if (!File.Exists(path))
        {
            return;
        }

        using var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        if (IsExtended(fs))
        {
            return;
        }

        Log.Information($"Extending '{path}'");

        Extend(fs);

        Log.Debug("Done");
    }

    public static bool IsExtended(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return IsExtended(fs);
    }

    public static bool IsExtended(FileStream fs)
    {
        if (fs.Length < ExtendedBaseAddress || fs.Length < ExtendedHeaderOffset + HeaderPatch.Length)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[HeaderPatch.Length];
        fs.Seek(ExtendedHeaderOffset, SeekOrigin.Begin);
        int read = fs.Read(header);

        return read == HeaderPatch.Length && header.SequenceEqual(HeaderPatch);
    }

    public static void Extend(FileStream fs)
    {
        long oldLength = fs.Length;
        long newLength = oldLength + ExtendedPadding;

        fs.SetLength(newLength);

        // Zero-fill between oldLength and ExtendedBaseAddress (if needed)
        if (oldLength < ExtendedBaseAddress)
        {
            fs.Seek(oldLength, SeekOrigin.Begin);
            long zeroesNeeded = ExtendedBaseAddress - oldLength;
            byte[] zeroChunk = new byte[Math.Min(64 * 1024, zeroesNeeded)];
            while (zeroesNeeded > 0)
            {
                int toWrite = (int)Math.Min(zeroChunk.Length, zeroesNeeded);
                fs.Write(zeroChunk, 0, toWrite);
                zeroesNeeded -= toWrite;
            }
        }

        // Fill from ExtendedBaseAddress (0x07000000) to newLength with 0xFF
        if (newLength > ExtendedBaseAddress)
        {
            long ffStart = Math.Max(oldLength, ExtendedBaseAddress);
            fs.Seek(ffStart, SeekOrigin.Begin);
            long ffNeeded = newLength - ffStart;
            byte[] ffChunk = new byte[Math.Min(64 * 1024, ffNeeded)];
            Array.Fill(ffChunk, (byte)0xFF);
            while (ffNeeded > 0)
            {
                int toWrite = (int)Math.Min(ffChunk.Length, ffNeeded);
                fs.Write(ffChunk, 0, toWrite);
                ffNeeded -= toWrite;
            }
        }

        // Patch ELF header at 0x60
        fs.Seek(ExtendedHeaderOffset, SeekOrigin.Begin);
        fs.Write(HeaderPatch, 0, HeaderPatch.Length);
        fs.Flush();
    }
}
