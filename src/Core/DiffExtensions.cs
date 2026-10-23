using DiffPatch.Data;
using System.IO;
using System.Text;

namespace MCEPatcher.Core;

public static class DiffExtensions
{
    public static bool IsHex(this FileDiff filePatch)
        => Path.GetExtension(filePatch.From) is ".hexdump" || Path.GetExtension(filePatch.To) is ".hexdump";

    public static string ToString(FileDiff diff, bool thisOnly = false)
    {
        var builder = new StringBuilder();

        builder.Append("--- ").AppendLine(diff.From);
        builder.Append("+++ ").AppendLine(diff.To);

        if (!thisOnly)
        {
            foreach (var chunk in diff.Chunks)
            {
                ToString(chunk, builder);
            }
        }

        return builder.ToString();
    }

    public static string ToString(Chunk chunk, bool thisOnly = false)
    {
        var builder = new StringBuilder();
        ToString(chunk, builder, thisOnly);

        return builder.ToString();
    }

    public static void ToString(Chunk chunk, StringBuilder builder, bool thisOnly = false)
    {
        builder.Append("@@ -");
        WriteRange(chunk.RangeInfo.OriginalRange);
        builder.Append("+");
        WriteRange(chunk.RangeInfo.NewRange);
        builder.AppendLine("@@");

        if (!thisOnly)
        {
            foreach (var lineDiff in chunk.Changes)
            {
                ToString(lineDiff, builder);
            }
        }

        void WriteRange(ChunkRange range)
        {
            builder.Append(range.StartLine);
            if (range.LineCount is not 1)
            {
                builder.Append(",").Append(range.LineCount);
            }

            builder.Append(" ");
        }
    }

    public static string ToString(LineDiff diff)
    {
        var builder = new StringBuilder();
        ToString(diff, builder);

        return builder.ToString();
    }

    public static void ToString(LineDiff diff, StringBuilder builder)
    {
        switch (diff.Type)
        {
            case LineChangeType.Add:
                builder.Append("+");
                break;
            case LineChangeType.Delete:
                builder.Append("-");
                break;
            default:
                builder.Append(" ");
                break;
        }

        builder.AppendLine(diff.Content);
    }
}
