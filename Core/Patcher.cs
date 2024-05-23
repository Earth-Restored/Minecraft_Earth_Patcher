using DiffPatch.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCEPatcher.Core
{
    public class Patcher
    {
        private string patchesLoation;
        private string filesLocation;

        private HashSet<string> hexDumpFiles = new HashSet<string>();

        public Patcher(string _patchesLoation, string _filesLocation = "")
        {
            patchesLoation = _patchesLoation;
            filesLocation = _filesLocation;
        }

        public void Patch(IEnumerable<string> patchesNames, Dictionary<string, string> variables, Dictionary<string, PatchInfo>? appliedPatches = null)
        {
            PatchContext context = new PatchContext(appliedPatches ?? new(), variables);

            patchAll(patchesNames, context);

            if (hexDumpFiles.Count != 0)
            {
                Log.Information("Undoing hex dumps");
                foreach (var file in hexDumpFiles)
                {
                    File.WriteAllBytes(file.Substring(0, file.LastIndexOf('.')), HexDump.Undo(File.ReadAllText(file)));

                    // delete the original file
                    File.Delete(file);
                }

                hexDumpFiles.Clear();
                Log.Debug("Done");
            }

            bool patchedVariables = false;

            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            foreach (var (name, info) in context.AppliedPatches)
            {
                if (info.BinaryVariables.Count == 0) continue;

                Log.Information($"Patching variables for patch '{name}'");
                patchedVariables = true;

                foreach (var variable in info.BinaryVariables)
                {
                    string val = variable.TemplateString;
                    foreach (var varName in info.VariablesUsed)
                    {
                        if (context.Variables.TryGetValue(varName, out string? value))
                            val = val.Replace($"${{{varName}}}", value.ToLowerInvariant());
                        else
                            throw new Exception($"Variable '{varName}' doesn't exist");
                    }

                    byte[] valBytes = Encoding.ASCII.GetBytes(val + "\0");
                    byte[] lengthBytes = BitConverter.GetBytes(valBytes.Length - 1);

                    if (valBytes.Length > 8 * 16 - 4)
                        throw new Exception($"String '{val}' is too long, max length allowed is: {8 * 16 - 4}");

                    string file = Path.Combine(filesLocation, variable.File);
                    if (!files.TryGetValue(file, out byte[]? bytes))
                    {
                        bytes = File.ReadAllBytes(file);
                        files.Add(file, bytes);
                    }

                    int index = variable.Address;

                    // write length
                    for (int i = 0; i < 4; i++)
                        bytes[index++] = lengthBytes[i];
                    // write the string
                    for (int i = 0; i < valBytes.Length; i++)
                        bytes[index++] = valBytes[i];
                }
            }
            if (patchedVariables)
                Log.Debug("Done");

            if (files.Count != 0)
            {
                Log.Debug("Writing files");
                foreach (var (file, bytes) in files)
                    File.WriteAllBytes(file, bytes);
                Log.Debug("Done");
            }
        }

        private void patchAll(IEnumerable<string> patchNames, PatchContext context)
        {
            var patches = loadPatches(patchNames);

            foreach (var (name, info) in patches)
            {
                if (context.AppliedPatches.ContainsKey(name)) continue;

                patch(name, info, context);
            }
        }

        private void patch(string patchName, PatchInfo info, PatchContext context)
        {
            foreach (var pre in info.Prerequisites)
                if (!pre.Check(context, out var patches))
                    patchAll(patches, context); // TODO: this could cause infinite loop, add depth (limit)? or check for it in Patch?

            string path = Path.Combine(patchesLoation, $"{patchName}.patch");
            if (!File.Exists(path))
            {
                Log.Fatal($"Patch '{patchName}' doesn't exist");
                U.PAKE();
                Environment.Exit(1);
                return;
            }

            Log.Information($"Applying patch '{patchName}'");
            patch(File.ReadAllText(path), patchName, context.Variables.Where(variable => info.VariablesUsed.Contains(variable.Key)).ToDictionary(item => item.Key, item => item.Value));
            Log.Debug($"Done");

            context.AppliedPatches.Add(patchName, info);
        }

        private void patch(string patch, string patchName, Dictionary<string, string>? variables = null)
        {
            // TODO: detect the new line used, could be differend then Environment.NewLine
            var filesToPatch = DiffPatch.DiffParserHelper.Parse(patch, Environment.NewLine);

            if (variables is not null && variables.Count != 0)
            {
                IEnumerable<FileDiff> textPatches = filesToPatch.Where(filePatch => !filePatch.IsHex());
                IEnumerable<FileDiff> hexPatches = filesToPatch.Where(filePatch => filePatch.IsHex());

                StringBuilder sb = new StringBuilder();

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
                    sb.Append(U.ToString(filePatch));

                string s = sb.ToString();
                filesToPatch = DiffPatch.DiffParserHelper.Parse(sb.ToString(), Environment.NewLine);
            }

            foreach (var filePatch in filesToPatch)
                patchFile(filePatch, patchName);
        }

        private void patchFile(FileDiff patch, string patchName)
        {
            if (patch.From != patch.To)
                throw new InvalidDataException($"patch.From ({patch.From}) must match patch.To ({patch.To})");

            FileInfo file = new FileInfo(Path.Combine(filesLocation, patch.From));

            file.Directory?.Create();

            bool hex = patch.IsHex();

            if (hex && !file.Exists)
            {
                string _from = file.FullName.Substring(0, file.FullName.LastIndexOf('.')); // remove .hexdump
                Log.Information($"Creating hexdump for '{_from}'");
                File.WriteAllText(file.FullName, HexDump.Create(File.ReadAllBytes(_from)));
                Log.Debug("Done");
                file.Refresh();
            }

            if (!file.Exists)
                throw new IOException($"File '{file.FullName}' doesn't exist, it is used by patch '{patchName}'");

            List<string> lines = File.ReadAllLines(file.FullName).ToList();

            foreach (var chunk in patch.Chunks)
                patchChunk(chunk, lines);

            File.WriteAllLines(file.FullName, lines);

            if (hex)
                hexDumpFiles.Add(file.FullName);
        }

        private void patchChunk(Chunk chunk, List<string> lines)
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

        static Dictionary<string, PatchInfo> loadPatches(IEnumerable<string> patchNames)
        {
            Dictionary<string, PatchInfo> patches = new Dictionary<string, PatchInfo>();

            foreach (string name in patchNames)
            {
                if (patches.ContainsKey(name)) continue;

                string path = Path.Combine("Patches", $"{name}.patch.info");

                patches.Add(name, PatchInfo.Load(path, name));
            }

            return patches;
        }

        public class PatchContext
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
}
