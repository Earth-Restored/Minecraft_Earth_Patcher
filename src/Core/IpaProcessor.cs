using Serilog;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace MCEPatcher.Core;

public static class IpaProcessor
{
    public static readonly string IpaHash = "91E235E52D430123FB73479512AD8BBE238518729BE9C6A0F372B9AFC876573B";

    internal static bool Autonomous { get; private set; }

    public static bool VerifyHash(Stream stream)
    {
        byte[] hashBytes = SHA256.HashData(stream);
        var hash = Convert.ToHexString(hashBytes);
        return hash == IpaHash;
    }

    public static async Task<bool> Run(Options options)
    {
        Autonomous = options.Autonomous;

        string inIpa = options.InIpa;
        string outIpa = options.OutIpa;
        string decodedDir = options.DecodedDir;

        if (!File.Exists(inIpa))
        {
            Log.Fatal($"In ipa '{inIpa}' doesn't exist");
            return false;
        }

        string protocol = options.Protocol == 0 ? "http://" : "https://";
        string baseUrl = $"{protocol}{options.Hostname}";

        Log.Information("*****Extracting IPA*****");
        if (Directory.Exists(decodedDir))
            Directory.Delete(decodedDir, true);

        ZipFile.ExtractToDirectory(inIpa, decodedDir);
        Log.Debug("Done");

        string payloadDir = Path.Combine(decodedDir, "Payload");
        if (!Directory.Exists(payloadDir))
        {
            Log.Fatal("IPA doesn't contain a Payload directory");
            return false;
        }

        var appDirs = Directory.GetDirectories(payloadDir, "*.app");
        if (appDirs.Length == 0)
        {
            Log.Fatal("No .app bundle found in Payload");
            return false;
        }

        string appDir = appDirs[0];
        string exePath = Path.Combine(appDir, "minecraftearthtf");

        if (!File.Exists(exePath))
        {
            Log.Fatal($"Main executable not found at '{exePath}'");
            return false;
        }

        Log.Information("Patching locator address");
        byte[] searchBytes = Encoding.ASCII.GetBytes("https://locator.mceserv.net");
        byte[] replaceBytes = AsciiToFixedBytes(baseUrl, 27);
        GlobalReplaceBytes(exePath, searchBytes, replaceBytes);
        Log.Debug("Done");

        Log.Information("Disabling sunset time check");
        byte[] sunsetPatch = { 0xE1, 0x05, 0x00, 0x54 };
        PatchAtOffset(exePath, 0x1129080, sunsetPatch);
        Log.Debug("Done");

        Log.Information("Removing DRM and code signature");
        RemoveDrm(appDir);
        Log.Debug("Done");

        if (!string.IsNullOrWhiteSpace(options.AppName))
        {
            Log.Information($"Changing app name to '{options.AppName}'");
            PatchAppName(appDir, options.AppName);
            Log.Debug("Done");
        }

        Log.Information("*****Building IPA*****");
        if (File.Exists(outIpa))
            File.Delete(outIpa);

        ZipFile.CreateFromDirectory(decodedDir, outIpa);

        if (Directory.Exists(decodedDir))
            Directory.Delete(decodedDir, true);

        Log.Information("Finished");
        return true;
    }

    private static byte[] AsciiToFixedBytes(string input, int maxLen)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(input);
        if (bytes.Length > maxLen)
            Array.Resize(ref bytes, maxLen);
        else if (bytes.Length < maxLen)
            Array.Resize(ref bytes, maxLen);
        return bytes;
    }

    private static void GlobalReplaceBytes(string filePath, byte[] search, byte[] replace)
    {
        byte[] data = File.ReadAllBytes(filePath);
        for (int i = 0; i <= data.Length - search.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < search.Length; j++)
            {
                if (data[i + j] != search[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                Array.Copy(replace, 0, data, i, replace.Length);
                i += replace.Length - 1;
            }
        }
        File.WriteAllBytes(filePath, data);
    }

    private static void PatchAtOffset(string filePath, long offset, byte[] data)
    {
        using (var fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
        {
            fs.Seek(offset, SeekOrigin.Begin);
            fs.Write(data, 0, data.Length);
        }
    }

    private static void RemoveDrm(string appDir)
    {
        string[] drmDirs = { "_CodeSignature", "SC_Info" };
        foreach (var dir in drmDirs)
        {
            string path = Path.Combine(appDir, dir);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    private static void PatchAppName(string appDir, string newName)
    {
        string plistPath = Path.Combine(appDir, "Info.plist");
        if (!File.Exists(plistPath))
            return;

        var doc = XDocument.Load(plistPath);
        var dict = doc.Root?.Element("dict");
        if (dict == null) return;

        var elements = dict.Elements().ToList();
        for (int i = 0; i < elements.Count - 1; i++)
        {
            if (elements[i].Name == "key" && (elements[i].Value == "CFBundleDisplayName" || elements[i].Value == "CFBundleName"))
            {
                if (elements[i + 1].Name == "string")
                    elements[i + 1].Value = newName;
            }
        }

        doc.Save(plistPath);
    }

#pragma warning disable CS8618
    public sealed class Options
    {
        public bool Autonomous { get; set; }
        public string InIpa { get; set; }
        public string OutIpa { get; set; }
        public int Protocol { get; set; }
        public string Hostname { get; set; }
        public string AppName { get; set; }
        public string DecodedDir { get; set; } = "IpaDecoded";
    }
#pragma warning restore CS8618
}
