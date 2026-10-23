using DiffPatch.Data;
using Serilog;
using System.Text;

namespace MCEPatcher.Core;

public sealed class Patcher
{
    private string patchesLoation;
    private string filesLocation;

    private readonly Dictionary<string, FileStream> binaryStreams = new();

    public Patcher(string _patchesLoation, string _filesLocation = "")
    {
        patchesLoation = _patchesLoation;
        filesLocation = _filesLocation;
    }

    private FileStream GetBinaryStream(string fullPath)
    {
        if (!binaryStreams.TryGetValue(fullPath, out var stream))
        {
            stream = new FileStream(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            binaryStreams.Add(fullPath, stream);
        }
        return stream;
    }

    public void Patch(IEnumerable<string> patchesNames, Dictionary<string, string> variables, Dictionary<string, PatchInfo>? appliedPatches = null)
    {
        var context = new PatchContext(appliedPatches ?? new(), variables);

        try
        {
            PatchAll(patchesNames, context);

            bool patchedVariables = false;

            foreach (var (name, info) in context.AppliedPatches)
            {
                if (info.BinaryVariables.Count is 0)
                {
                    continue;
                }

                Log.Information($"Patching variables for patch '{name}'");
                patchedVariables = true;

                foreach (var variable in info.BinaryVariables)
                {
                    var val = variable.TemplateString;

                    foreach (var varName in info.VariablesUsed)
                    {
                        if (context.Variables.TryGetValue(varName, out string? value))
                        {
                            val = val.Replace($"${{{varName}}}", value.ToLowerInvariant());
                        }
                        else
                        {
                            throw new Exception($"Variable '{varName}' doesn't exist");
                        }
                    }

                    var valBytes = Encoding.ASCII.GetBytes(val + "\0");
                    var lengthBytes = BitConverter.GetBytes(valBytes.Length - 1);

                    if (valBytes.Length > 8 * 16 - 4)
                    {
                        throw new Exception($"String '{val}' is too long, max length allowed is: {8 * 16 - 4}");
                    }

                    var file = Path.Combine(filesLocation, variable.File);
                    var stream = GetBinaryStream(file);

                    stream.Seek(variable.Address, SeekOrigin.Begin);
                    stream.Write(lengthBytes, 0, 4);
                    stream.Write(valBytes, 0, valBytes.Length);
                }
            }
            if (patchedVariables)
            {
                Log.Debug("Done");
            }
        }
        finally
        {
            if (binaryStreams.Count is not 0)
            {
                Log.Debug("Flushing binary files");
                foreach (var stream in binaryStreams.Values)
                {
                    stream.Flush();
                    stream.Dispose();
                }
                binaryStreams.Clear();
                Log.Debug("Done");
            }
        }
    }

    private void PatchAll(IEnumerable<string> patchNames, PatchContext context)
    {
        var patches = LoadPatches(patchNames);

        foreach (var (name, info) in patches)
        {
            if (context.AppliedPatches.ContainsKey(name))
            {
                continue;
            }

            Patch(name, info, context);
        }
    }

    private void Patch(string patchName, PatchInfo info, PatchContext context)
    {
        foreach (var pre in info.Prerequisites)
        {
            if (!pre.Check(context, out var patches))
            {
                PatchAll(patches, context); // TODO: this could cause infinite loop, add depth (limit)? or check for it in Patch?
            }
        }

        if (patchName == ExtendLibgenoa.Name)
        {
            Log.Information($"Applying patch '{patchName}'");
            string libgenoaPath = Path.Combine(filesLocation, "lib", "arm64-v8a", "libgenoa.so");
            if (File.Exists(libgenoaPath))
            {
                var stream = GetBinaryStream(libgenoaPath);
                if (!ExtendLibgenoa.IsExtended(stream))
                {
                    ExtendLibgenoa.Extend(stream);
                }
            }
            else
            {
                ExtendLibgenoa.Extent(new DirectoryInfo(filesLocation));
            }
            Log.Debug("Done");
            context.AppliedPatches.Add(patchName, info);
            return;
        }

        string? patchContent = PatchResourceLoader.TryGetPatchContent(patchesLoation, patchName);
        if (patchContent is null)
        {
            throw new FileNotFoundException($"Patch '{patchName}' doesn't exist at '{Path.Combine(patchesLoation, $"{patchName}.patch")}' or as an embedded resource");
        }

        Log.Information($"Applying patch '{patchName}'");
        Patch(patchContent, patchName, context.Variables.Where(variable => info.VariablesUsed.Contains(variable.Key)).ToDictionary(item => item.Key, item => item.Value));
        Log.Debug($"Done");

        context.AppliedPatches.Add(patchName, info);
    }

    private void Patch(string patch, string patchName, Dictionary<string, string>? variables = null)
    {
        var newLine = U.GetNewLine(patch);
        var filesToPatch = Parse(patch);

        if (variables is not null && variables.Count is not 0)
        {
            var textPatches = filesToPatch.Where(filePatch => !filePatch.IsHex());
            var hexPatches = filesToPatch.Where(filePatch => filePatch.IsHex());

            var sb = new StringBuilder();

            foreach (var filePatch in textPatches)
            {
                string str = U.ToString(filePatch);

                foreach (var item in variables)
                {
                    var (name, value) = (item.Key, item.Value);

                    str = str.Replace($"${{{name}}}", value);
                }

                sb.Append(str);
            }

            foreach (var filePatch in hexPatches)
            {
                sb.Append(U.ToString(filePatch));
            }

            filesToPatch = Parse(sb.ToString());
        }

        foreach (var filePatch in filesToPatch)
        {
            PatchFile(filePatch, patchName);
        }
    }

    private void PatchFile(FileDiff patch, string patchName)
    {
        if (patch.From != patch.To)
        {
            throw new InvalidDataException($"patch.From ({patch.From}) must match patch.To ({patch.To})");
        }

        if (patch.IsHex())
        {
            PatchHexDiff(patch, patchName);
            return;
        }

        var file = new FileInfo(Path.Combine(filesLocation, patch.From));

        file.Directory?.Create();

        if (!file.Exists)
        {
            throw new IOException($"File '{file.FullName}' doesn't exist, it is used by patch '{patchName}'");
        }

        List<string> lines = File.ReadAllLines(file.FullName).ToList();

        foreach (var chunk in patch.Chunks)
        {
            PatchChunk(chunk, lines);
        }

        File.WriteAllLines(file.FullName, lines);
    }

    private void PatchHexDiff(FileDiff patch, string patchName)
    {
        string relBinaryPath = patch.From.Substring(0, patch.From.LastIndexOf('.'));
        string fullBinaryPath = Path.Combine(filesLocation, relBinaryPath);

        if (!File.Exists(fullBinaryPath))
        {
            throw new FileNotFoundException($"Binary file '{fullBinaryPath}' doesn't exist, it is used by patch '{patchName}'");
        }

        var stream = GetBinaryStream(fullBinaryPath);

        if (relBinaryPath.EndsWith("libgenoa.so") && !ExtendLibgenoa.IsExtended(stream))
        {
            ExtendLibgenoa.Extend(stream);
        }

        Span<byte> lineBytes = stackalloc byte[16];

        foreach (var chunk in patch.Chunks)
        {
            foreach (var change in chunk.Changes)
            {
                if (change.Type != LineChangeType.Add)
                {
                    continue;
                }

                if (!TryParseHexDumpLine(change.Content, out long offset, lineBytes, out int bytesParsed))
                {
                    throw new InvalidDataException($"Failed to parse hexdump line in patch '{patchName}': {change.Content}");
                }

                if (offset + bytesParsed > stream.Length)
                {
                    throw new InvalidDataException($"Hex patch offset 0x{offset:X} + {bytesParsed} is out of bounds for '{fullBinaryPath}' (size: {stream.Length}) in patch '{patchName}'");
                }

                stream.Seek(offset, SeekOrigin.Begin);
                stream.Write(lineBytes.Slice(0, bytesParsed));
            }
        }
    }

    private static bool TryParseHexDumpLine(string line, out long offset, Span<byte> outBuffer, out int bytesParsed)
    {
        offset = 0;
        bytesParsed = 0;
        line = line.TrimEnd('\r', '\n');
        if (line.Length < 10)
        {
            return false;
        }

        if (!long.TryParse(line.AsSpan(0, 8), System.Globalization.NumberStyles.HexNumber, null, out offset))
        {
            return false;
        }

        // Group 1 (up to 8 bytes, starts at index 10)
        for (int i = 0; i < 8; i++)
        {
            int idx = 10 + i * 3;
            if (idx + 2 > line.Length) break;
            var span = line.AsSpan(idx, 2);
            if (span[0] == ' ') break;
            if (byte.TryParse(span, System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                outBuffer[bytesParsed++] = b;
            }
            else break;
        }

        // Group 2 (up to 8 bytes, starts at index 35)
        for (int i = 0; i < 8; i++)
        {
            int idx = 35 + i * 3;
            if (idx + 2 > line.Length) break;
            var span = line.AsSpan(idx, 2);
            if (span[0] == ' ') break;
            if (byte.TryParse(span, System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                outBuffer[bytesParsed++] = b;
            }
            else break;
        }

        return bytesParsed > 0;
    }

    private void PatchChunk(Chunk chunk, List<string> lines)
    {
        ChunkRange range = chunk.RangeInfo.NewRange;

        int lineIndex = range.StartLine - 1;
        foreach (var change in chunk.Changes)
        {
            switch (change.Type)
            {
                case LineChangeType.Normal:
                    lineIndex++;
                    break;
                case LineChangeType.Add:
                    lines.Insert(lineIndex++, change.Content.Replace("\r", string.Empty).Replace("\n", string.Empty));
                    break;
                case LineChangeType.Delete:
                    lines.RemoveAt(lineIndex);
                    break;
                default:
                    throw new InvalidDataException($"Unknown {nameof(LineChangeType)}: '{change.Type}'");
            }
        }
    }

    private Dictionary<string, PatchInfo> LoadPatches(IEnumerable<string> patchNames)
    {
        var patches = new Dictionary<string, PatchInfo>();

        foreach (string name in patchNames)
        {
            if (patches.ContainsKey(name))
            {
                continue;
            }

            string path = Path.Combine(patchesLoation, $"{name}.patch.info");

            patches.Add(name, PatchInfo.Load(path, name));
        }

        return patches;
    }

    private static IEnumerable<FileDiff> Parse(string patch)
    {
        if (string.IsNullOrWhiteSpace(patch))
        {
            return [];
        }

        IEnumerable<string> enumerable = SplitLines(patch);
        if (!enumerable.Any())
        {
            return [];
        }

        return new DiffParser().Run(enumerable);

        IEnumerable<string> SplitLines(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return [];
            }

            string[] array = input.Split(["\r\n", "\n"], StringSplitOptions.None);
            if (array.Length is not 0)
            {
                return array;
            }

            return [];
        }
    }

    public sealed class PatchContext
    {
        public readonly Dictionary<string, PatchInfo> AppliedPatches;
        public readonly Dictionary<string, string> Variables;

        public PatchContext(Dictionary<string, PatchInfo> _appliedPatches, Dictionary<string, string> _variables)
        {
            AppliedPatches = _appliedPatches;
            Variables = _variables;
        }
    }
}
