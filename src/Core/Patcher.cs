using DiffPatch.Data;
using Serilog;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace MCEPatcher.Core;

public sealed partial class Patcher
{
    private string _patchesLoation;
    private string _filesLocation;

    private readonly Dictionary<string, FileStream> binaryStreams = [];

    public Patcher(string patchesLoation, string filesLocation = "")
    {
        _patchesLoation = patchesLoation;
        _filesLocation = filesLocation;
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
        var context = new PatchContext(appliedPatches ?? [], variables);

        try
        {
            PatchAll(patchesNames, context);

            var patchedVariables = false;

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
                    var value = variable.TemplateString;

                    foreach (var variableName in info.VariablesUsed)
                    {
                        if (context.Variables.TryGetValue(variableName, out string? variableValue))
                        {
                            value = value.Replace($"${{{variableName}}}", variableValue.ToLowerInvariant());
                        }
                        else
                        {
                            throw new Exception($"Variable '{variableName}' doesn't exist");
                        }
                    }

                    var valueBytes = Encoding.ASCII.GetBytes(value + "\0");
                    var lengthBytes = BitConverter.GetBytes(valueBytes.Length - 1);

                    if (valueBytes.Length > 8 * 16 - 4)
                    {
                        throw new Exception($"String '{value}' is too long, max length allowed is: {8 * 16 - 4}");
                    }

                    var file = Path.Combine(_filesLocation, variable.File);
                    var stream = GetBinaryStream(file);

                    stream.Seek(variable.Address, SeekOrigin.Begin);
                    stream.Write(lengthBytes, 0, 4);
                    stream.Write(valueBytes, 0, valueBytes.Length);
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

        if (patchName is ExtendLibgenoa.Name)
        {
            Log.Information($"Applying patch '{patchName}'");
            var libgenoaPath = Path.Combine(_filesLocation, "lib", "arm64-v8a", "libgenoa.so");

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
                ExtendLibgenoa.Extent(new DirectoryInfo(_filesLocation));
            }

            Log.Debug("Done");
            context.AppliedPatches.Add(patchName, info);
            return;
        }

        var patchContent = PatchResourceLoader.TryGetPatchContent(_patchesLoation, patchName);
        if (patchContent is null)
        {
            throw new FileNotFoundException($"Patch '{patchName}' doesn't exist at '{Path.Combine(_patchesLoation, $"{patchName}.patch")}' or as an embedded resource.");
        }

        Log.Information($"Applying patch '{patchName}'");
        Patch(patchContent, patchName, context.Variables.Where(variable => info.VariablesUsed.Contains(variable.Key)).ToDictionary(item => item.Key, item => item.Value));
        Log.Debug($"Done");

        context.AppliedPatches.Add(patchName, info);
    }

    private void Patch(string patch, string patchName, Dictionary<string, string>? variables = null)
    {
        var newLine = StringExtensions.GetNewLine(patch);
        var filesToPatch = Parse(patch);

        if (variables is not null && variables.Count is not 0)
        {
            var textPatches = filesToPatch.Where(filePatch => !filePatch.IsHex());
            var hexPatches = filesToPatch.Where(filePatch => filePatch.IsHex());

            var builder = new StringBuilder();

            foreach (var filePatch in textPatches)
            {
                string str = DiffExtensions.ToString(filePatch);

                foreach (var item in variables)
                {
                    var (name, value) = (item.Key, item.Value);

                    str = str.Replace($"${{{name}}}", value);
                }

                builder.Append(str);
            }

            foreach (var filePatch in hexPatches)
            {
                builder.Append(DiffExtensions.ToString(filePatch));
            }

            filesToPatch = Parse(builder.ToString());
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

        var file = new FileInfo(Path.Combine(_filesLocation, patch.From));

        file.Directory?.Create();

        if (!file.Exists)
        {
            throw new IOException($"File '{file.FullName}' doesn't exist, it is used by patch '{patchName}'");
        }

        var lines = File.ReadAllLines(file.FullName).ToList();

        if (file.Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
        {
            lines = NormalizeXmlLines(lines);
        }

        foreach (var chunk in patch.Chunks)
        {
            PatchChunk(chunk, lines, patch.From);
        }

        File.WriteAllLines(file.FullName, lines);
    }

    private void PatchHexDiff(FileDiff patch, string patchName)
    {
        var relativeBinaryPath = patch.From.Substring(0, patch.From.LastIndexOf('.'));
        var fullBinaryPath = Path.Combine(_filesLocation, relativeBinaryPath);

        if (!File.Exists(fullBinaryPath))
        {
            throw new FileNotFoundException($"Binary file '{fullBinaryPath}' doesn't exist, it is used by patch '{patchName}'");
        }

        var stream = GetBinaryStream(fullBinaryPath);

        if (relativeBinaryPath.EndsWith("libgenoa.so") && !ExtendLibgenoa.IsExtended(stream))
        {
            ExtendLibgenoa.Extend(stream);
        }

        Span<byte> lineBytes = stackalloc byte[16];

        foreach (var chunk in patch.Chunks)
        {
            foreach (var change in chunk.Changes)
            {
                if (change.Type is not LineChangeType.Add)
                {
                    continue;
                }

                if (!TryParseHexDumpLine(change.Content, out long offset, lineBytes, out int bytesParsed))
                {
                    throw new InvalidDataException($"Failed to parse hexdump line in patch '{patchName}': {change.Content}.");
                }

                if (offset + bytesParsed > stream.Length)
                {
                    throw new InvalidDataException($"Hex patch offset 0x{offset:X} + {bytesParsed} is out of bounds for '{fullBinaryPath}' (size: {stream.Length}) in patch '{patchName}'.");
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
        for (var i = 0; i < 8; i++)
        {
            var idx = 10 + i * 3;
            if (idx + 2 > line.Length)
            {
                break;
            }

            var span = line.AsSpan(idx, 2);
            if (span[0] is ' ')
            {
                break;
            }

            if (byte.TryParse(span, System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                outBuffer[bytesParsed++] = b;
            }
            else
            {
                break;
            }
        }

        // Group 2 (up to 8 bytes, starts at index 35)
        for (var i = 0; i < 8; i++)
        {
            var idx = 35 + i * 3;
            if (idx + 2 > line.Length)
            {
                break;
            }

            var span = line.AsSpan(idx, 2);
            if (span[0] is ' ')
            {
                break;
            }

            if (byte.TryParse(span, System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                outBuffer[bytesParsed++] = b;
            }
            else
            {
                break;
            }
        }

        return bytesParsed > 0;
    }

    private static List<string> NormalizeXmlLines(List<string> inputLines)
    {
        var result = new List<string>(inputLines.Count);
        var builder = new StringBuilder();
        var inTag = false;

        foreach (var line in inputLines)
        {
            if (inTag)
            {
                builder.Append(' ').Append(line.Trim());
                if (line.Contains('>'))
                {
                    inTag = false;
                    result.Add(builder.ToString());
                    builder.Clear();
                }
            }
            else
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith('<') && !trimmed.StartsWith("<!--") && !line.Contains('>'))
                {
                    inTag = true;
                    builder.Append(line);
                }
                else
                {
                    result.Add(line);
                }
            }
        }

        if (builder.Length > 0)
        {
            result.Add(builder.ToString());
        }

        return result;
    }

    private void PatchChunk(Chunk chunk, List<string> lines, string filePath)
    {
        var beforeChanges = chunk.Changes.Where(c => c.Type is LineChangeType.Normal or LineChangeType.Delete).ToList();
        var afterChanges = chunk.Changes.Where(c => c.Type is LineChangeType.Normal or LineChangeType.Add).ToList();

        var beforeLines = beforeChanges.Select(c => c.Content.TrimEnd('\r', '\n')).ToList();
        var afterLines = afterChanges.Select(c => c.Content.TrimEnd('\r', '\n')).ToList();

        var originalStart = chunk.RangeInfo.OriginalRange.StartLine;
        var targetStart = originalStart > 0 ? originalStart - 1 : 0;

        // Check if chunk has already been applied
        if (afterLines.Count > 0 && FindSequenceIndex(lines, targetStart, afterLines) != -1)
        {
            Log.Debug("Chunk at line {StartLine} in '{FilePath}' is already applied, skipping.", originalStart, filePath);
            return;
        }

        if (beforeLines.Count is 0)
        {
            var insertIndex = int.Clamp(targetStart, 0, lines.Count);

            foreach (var change in chunk.Changes)
            {
                if (change.Type == LineChangeType.Add)
                {
                    lines.Insert(insertIndex++, change.Content.TrimEnd('\r', '\n'));
                }
            }

            return;
        }

        var matchIndex = FindSequenceIndex(lines, targetStart, beforeLines);
        if (matchIndex is -1)
        {
            var expectedPreview = string.Join("\n", beforeLines.Take(5));
            var actualContextStart = int.Max(0, targetStart - 2);
            var actualContextCount = int.Min(lines.Count - actualContextStart, 10);
            var actualPreview = string.Join("\n", lines.Skip(actualContextStart).Take(actualContextCount));

            throw new InvalidDataException($"""
                Failed to apply patch chunk at original line {originalStart} for '{filePath}'.
                Could not find expected context.\nExpected context:\n{expectedPreview}\n\nActual context around line {targetStart + 1}:\n{actualPreview}.
                """);
        }

        var currentLine = matchIndex;

        foreach (var change in chunk.Changes)
        {
            switch (change.Type)
            {
                case LineChangeType.Normal:
                    {
                        if (!LineMatches(lines[currentLine], change.Content.TrimEnd('\r', '\n')))
                        {
                            throw new InvalidDataException($"Mismatch on context line at index {currentLine} in '{filePath}'. Expected '{change.Content.Trim()}', found '{lines[currentLine]}'.");
                        }

                        currentLine++;
                    }
                    break;

                case LineChangeType.Delete:
                    {
                        if (!LineMatches(lines[currentLine], change.Content.TrimEnd('\r', '\n')))
                        {
                            throw new InvalidDataException($"Mismatch on line to delete at index {currentLine} in '{filePath}'. Expected '{change.Content.Trim()}', found '{lines[currentLine]}'.");
                        }

                        lines.RemoveAt(currentLine);
                    }
                    break;

                case LineChangeType.Add:
                    {
                        lines.Insert(currentLine++, change.Content.TrimEnd('\r', '\n'));
                    }
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(change.Type), (int)change.Type, typeof(LineChangeType));
            }
        }
    }

    private static int FindSequenceIndex(List<string> lines, int targetStart, List<string> pattern)
    {
        if (pattern.Count is 0 || lines.Count < pattern.Count)
        {
            return -1;
        }

        var maxIndex = lines.Count - pattern.Count;
        var clampedStart = int.Clamp(targetStart, 0, maxIndex);

        var maxOffset = int.Max(clampedStart, maxIndex - clampedStart);
        for (var offset = 0; offset <= maxOffset; offset++)
        {
            var forward = clampedStart + offset;
            if (forward <= maxIndex && MatchesAt(lines, forward, pattern))
            {
                return forward;
            }

            var backward = clampedStart - offset;
            if (offset != 0 && backward >= 0 && MatchesAt(lines, backward, pattern))
            {
                return backward;
            }
        }

        return -1;
    }

    private static bool MatchesAt(List<string> lines, int startIndex, List<string> pattern)
    {
        for (var i = 0; i < pattern.Count; i++)
        {
            if (!LineMatches(lines[startIndex + i], pattern[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LineMatches(string actual, string expected)
    {
        if (actual == expected)
        {
            return true;
        }

        var actualTrim = actual.Trim();
        var expectedTrim = expected.Trim();
        if (actualTrim == expectedTrim)
        {
            return true;
        }

        var normActual = NormalizeLine(actualTrim);
        var normExpected = NormalizeLine(expectedTrim);
        if (normActual == normExpected)
        {
            return true;
        }

        if (TryMatchXmlTags(normActual, normExpected))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeLine(string line)
    {
        var normalized = WhitespaceRegex().Replace(line, " ");
        normalized = normalized.Replace(" />", "/>");
        normalized = HexNumberRegex().Replace(normalized, "0x$1");
        return normalized;
    }

    private static bool TryMatchXmlTags(string actual, string expected)
    {
        if (!actual.StartsWith('<') || !expected.StartsWith('<'))
        {
            return false;
        }

        if (!actual.EndsWith('>') || !expected.EndsWith('>'))
        {
            return false;
        }

        var actualTextMatch = XmlElementWithTextRegex().Match(actual);
        var expectedTextMatch = XmlElementWithTextRegex().Match(expected);
        if (actualTextMatch.Success || expectedTextMatch.Success)
        {
            if (!actualTextMatch.Success || !expectedTextMatch.Success)
            {
                return false;
            }

            var actualTag = actualTextMatch.Groups[1].Value;
            var expectedTag = expectedTextMatch.Groups[1].Value;
            if (!string.Equals(actualTag, expectedTag, StringComparison.Ordinal))
            {
                return false;
            }

            var actualText = actualTextMatch.Groups[3].Value.Trim();
            var expectedText = expectedTextMatch.Groups[3].Value.Trim();
            if (!string.Equals(actualText, expectedText, StringComparison.Ordinal))
            {
                return false;
            }

            return MatchAttributes(actualTextMatch.Groups[2].Value, expectedTextMatch.Groups[2].Value);
        }

        var actualSelfMatch = XmlSelfClosingRegex().Match(actual);
        var expectedSelfMatch = XmlSelfClosingRegex().Match(expected);
        if (actualSelfMatch.Success || expectedSelfMatch.Success)
        {
            if (!actualSelfMatch.Success || !expectedSelfMatch.Success)
            {
                return false;
            }

            var actualTag = actualSelfMatch.Groups[1].Value;
            var expectedTag = expectedSelfMatch.Groups[1].Value;
            if (!string.Equals(actualTag, expectedTag, StringComparison.Ordinal))
            {
                return false;
            }

            return MatchAttributes(actualSelfMatch.Groups[2].Value, expectedSelfMatch.Groups[2].Value);
        }

        var actualOpenMatch = XmlOpenTagRegex().Match(actual);
        var expectedOpenMatch = XmlOpenTagRegex().Match(expected);
        if (actualOpenMatch.Success || expectedOpenMatch.Success)
        {
            if (!actualOpenMatch.Success || !expectedOpenMatch.Success)
            {
                return false;
            }

            var actualTag = actualOpenMatch.Groups[1].Value;
            var expectedTag = expectedOpenMatch.Groups[1].Value;
            if (!string.Equals(actualTag, expectedTag, StringComparison.Ordinal))
            {
                return false;
            }


            return MatchAttributes(actualOpenMatch.Groups[2].Value, expectedOpenMatch.Groups[2].Value);
        }

        return false;
    }

    private static bool MatchAttributes(string actualAttrsStr, string expectedAttrsStr)
    {
        var actualAttributes = ParseAttributes(actualAttrsStr);
        var expectedAttributes = ParseAttributes(expectedAttrsStr);

        foreach (var kvp in expectedAttributes)
        {
            if (!actualAttributes.TryGetValue(kvp.Key, out var actualValue))
            {
                return false;
            }

            if (kvp.Value != actualValue)
            {
                var normExpectedVal = HexNumberRegex().Replace(kvp.Value, "0x$1");
                var normActualVal = HexNumberRegex().Replace(actualValue, "0x$1");

                if (normExpectedVal != normActualVal)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static Dictionary<string, string> ParseAttributes(string tagStr)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        var matches = XmlAttrRegex().Matches(tagStr);
        foreach (Match match in matches)
        {
            dict[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return dict;
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

            var path = Path.Combine(_patchesLoation, $"{name}.patch.info");

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

        var enumerable = SplitLines(patch);
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

    [GeneratedRegex(@"0x0+([0-9a-fA-F]+)")]
    private static partial Regex HexNumberRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^<([a-zA-Z0-9_\-:]+)((?:\s+[a-zA-Z0-9_\-:]+\s*=\s*""[^""]*"")*)\s*>(.*)</\1>$")]
    private static partial Regex XmlElementWithTextRegex();

    [GeneratedRegex(@"^<([a-zA-Z0-9_\-:]+)((?:\s+[a-zA-Z0-9_\-:]+\s*=\s*""[^""]*"")*)\s*/>$")]
    private static partial Regex XmlSelfClosingRegex();

    [GeneratedRegex(@"^<([a-zA-Z0-9_\-:]+)((?:\s+[a-zA-Z0-9_\-:]+\s*=\s*""[^""]*"")*)\s*>$")]
    private static partial Regex XmlOpenTagRegex();

    [GeneratedRegex(@"([a-zA-Z0-9_\-:]+)\s*=\s*""([^""]*)""")]
    private static partial Regex XmlAttrRegex();
}
