using Serilog;

namespace MCEPatcher.Core
{
    public static class ExtendLibgenoa
    {
        public const string Name = "extend-libgenoa";

        public static void Extent(DirectoryInfo decodedFiles)
        {
            string path = Path.Combine(decodedFiles.FullName, "lib", "arm64-v8a", "libgenoa.so");
            string target = path + ".hexdump";

            if (File.Exists(target)) return;

            byte[] _bytes = File.ReadAllBytes(path);

            byte[] bytes = new byte[_bytes.Length + 10965232];

            Array.ConstrainedCopy(_bytes, 0, bytes, 0, _bytes.Length);
            Array.Fill(bytes, (byte)0xFF, 0x07000000, bytes.Length - 0x07000000);

            Log.Information($"Creating hexdump for '{path}'");
            File.WriteAllText(target, HexDump.Create(bytes));
        }
    }
}
