using System.IO;

namespace MCEPatcher.Core;

public static class StreamExtensions
{
    public static byte[] ReadToEnd(this Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        return ms.ToArray();
    }
}
