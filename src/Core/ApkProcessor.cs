using CommandLine;
using Serilog;

namespace MCEPatcher.Core
{
    public static class ApkProcessor
    {
        internal static bool Autonomous { get; private set; }

        public static async Task<bool> Run(Options options)
        {
            Autonomous = options.Autonomous;
            HashSet<string> patches = new(options.Patches);

            FileInfo inApk = new FileInfo(options.InApk);
            FileInfo outApk = new FileInfo(options.OutApk);
            DirectoryInfo decodedDir = new DirectoryInfo(options.DecodedDir);

            if (!inApk.Exists)
            {
                Log.Fatal($"In apk '{inApk.FullName}' doesn't exist");
                return false;
            }

            Dictionary<string, string> variables = new();

            foreach (var item in options.Variables)
            {
                string[] split = item.Split('=');

                if (split.Length < 2)
                {
                    Log.Fatal($"Variable '{item}' doesn't have value (no '=')");
                    return false;
                }
                else if (split.Length > 2)
                {
                    Log.Fatal($"Too many '=' in variable ({item})");
                    return false;
                }

                var (name, value) = (split[0], split[1]);

                if (string.IsNullOrWhiteSpace(name))
                {
                    Log.Fatal($"Name of a variable can't be empty");
                    return false;
                }
                else if (variables.ContainsKey(name))
                {
                    Log.Fatal($"Variable '{name}' is defined multiple times");
                    return false;
                }

                variables.Add(name, value);
            }

            if (!options.SkipDecode || !options.SkipBuild)
            {
                await DependencyDownloader.Download("https://github.com/iBotPeaches/Apktool/releases/download/v3.0.1/apktool_3.0.1.jar", APK.FileName);
            }

            if (!options.SkipSign)
            {
                await DependencyDownloader.Download("https://github.com/patrickfav/uber-apk-signer/releases/download/v1.3.0/uber-apk-signer-1.3.0.jar", Signer.FileName);
            }

            if (options.SkipDecode)
                Log.Information("Decoding skipped");
            else
            {
                Log.Information("*****Decoding apk*****");
                if (!APK.Decode(inApk, decodedDir))
                {
                    return false;
                }

                if (!File.Exists(Path.Combine(decodedDir.FullName, "lib", "arm64-v8a", "libgenoa.so")))
                {
                    Log.Error("libgenoa.so does not exist, wrong apk?");
                    return false;
                }

                Log.Debug("Done");
            }

            // does not get properly extended if not done before the hexdump and will most likely be done anyway, TODO: remove once the TODO in Patcher.patchFile is fixed
            Log.Information($"Applying patch '{ExtendLibgenoa.Name}'");
            ExtendLibgenoa.Extent(decodedDir);
            patches.Remove(ExtendLibgenoa.Name);
            Log.Debug("Done");

            Patcher patcher = new Patcher("Patches", decodedDir.FullName);
            patcher.Patch(patches, variables, patches.Contains(ExtendLibgenoa.Name) ? new()
            {
                [ExtendLibgenoa.Name] = PatchInfo.Default
            } : new());

            if (options.SkipBuild)
                Log.Information($"Building skipped");
            else
            {
                Log.Information("*****Building apk*****");
                if (!APK.Encode(decodedDir, outApk))
                    return false;

                Log.Debug("Done");
            }

            if (options.SkipSign)
                Log.Information($"Signing skipped");
            else
            {
                Log.Information("*****Signing apk*****");
                if (!Signer.Sign(outApk, new DirectoryInfo("Signed")))
                    return false;

                Log.Debug("Done");
            }

            Log.Information("Finished");

            return true;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public class Options
        {
            [Option('i', "in-apk", Default = "Minecraft_Earth.apk", Required = false, HelpText = "Path to the minecraft earth apk")]
            public string InApk { get; set; }

            [Option('o', "out-apk", Default = "Minecraft_Earth_patched.apk", Required = false, HelpText = "Path of the patched apk")]
            public string OutApk { get; set; }

            [Option("decoded-dir", Default = "Decoded", Required = false, HelpText = "Path of the dir the input apk will be decoded to")]
            public string DecodedDir { get; set; }

            [Option('p', "patches", Required = true, HelpText = "List of patches to apply, in format '[patch1 name] \"[patch2 name]\" ...'")]
            public IEnumerable<string> Patches { get; set; }

            [Option('v', "variables", Required = true, HelpText = "Variables to use for patches, in format '[var1 name]=[var1 value] \"[var2 name]\"=\"[var2 value]\" ...'")]
            public IEnumerable<string> Variables { get; set; }

            [Option('a', "autonomous", Required = false, HelpText = "If true, no interaction from user is required")]
            public bool Autonomous { get; set; }

            [Option("skip-decode", Required = false, HelpText = "Skips decoding the apk")]
            public bool SkipDecode { get; set; }

            [Option("skip-build", Required = false, HelpText = "Skips building the apk")]
            public bool SkipBuild { get; set; }

            [Option("skip-sign", Required = false, HelpText = "Skips signing of the apk")]
            public bool SkipSign { get; set; }
        }
#pragma warning restore CS8618
    }
}
