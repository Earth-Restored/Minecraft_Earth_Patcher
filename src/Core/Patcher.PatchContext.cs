namespace MCEPatcher.Core;

public sealed partial class Patcher
{
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
