using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCEPatcher.Core
{
    public static class HexDump
    {
        public const int BytesPerLine = 16;
        public const int BytesPerLineHalf = BytesPerLine / 2;

        public static string CreateLine(IList<byte> bytes, int offset, int writeOffset = -1)
        {
            if (writeOffset == -1)
                writeOffset = offset;

            int length = Math.Min(16, bytes.Count - offset);

            StringBuilder sb = new StringBuilder();

            sb.Append(writeOffset.ToString("x").PadLeft(8, '0')).Append("  ");

            // bytes
            for (int i = 0; i < BytesPerLineHalf; i++)
                sb.Append(getByte(i)).Append(" ");

            sb.Append(" ");

            for (int i = 0; i < BytesPerLineHalf; i++)
                sb.Append(getByte(i + BytesPerLineHalf)).Append(" ");

            // text
            sb.Append(" |");
            for (int i = 0; i < BytesPerLine; i++)
                sb.Append(getChar(i));

            sb.AppendLine("|");

            return sb.ToString();

            string getByte(int index)
            {
                if (index < length) return bytes[index + offset].ToString("x2");
                else return "  ";
            }

            string getChar(int index)
            {
                if (index < length)
                {
                    char c = (char)bytes[index + offset];
                    if (c == ' ') return " ";
                    else if (char.IsControl(c) || c > 0b_0111_1111) return ".";
                    else return c.ToString();
                }
                else
                    return string.Empty;
            }
        }

        public static string Create(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();

            // TODO: only use getByte in last line
            int i;
            for (i = 0; i < bytes.Length; i += BytesPerLine)
            {
                // offset
                sb.Append(i.ToString("x").PadLeft(8, '0')).Append("  ");

                // bytes
                for (int j = 0; j < BytesPerLineHalf; j++)
                    sb.Append(getByte(i + j)).Append(" ");

                sb.Append(" ");

                for (int j = 0; j < BytesPerLineHalf; j++)
                    sb.Append(getByte(i + j + BytesPerLineHalf)).Append(" ");

                // text
                sb.Append(" |");
                for (int j = 0; j < BytesPerLine; j++)
                    sb.Append(getChar(i + j));

                sb.AppendLine("|");
            }

            sb.AppendLine(i.ToString("x").PadLeft(8, '0'));

            return sb.ToString();

            string getByte(int index)
            {
                if (index < bytes.Length) return bytes[index].ToString("x2");
                else return "  ";
            }

            string getChar(int index)
            {
                if (index < bytes.Length)
                {
                    char c = (char)bytes[index];
                    if (c == ' ') return " ";
                    else if (char.IsControl(c) || c > 0b_0111_1111) return ".";
                    else return c.ToString();
                }
                else
                    return string.Empty;
            }
        }

        public static byte[] Undo(string hexdump)
        {
            string newLine = U.GetNewLine(hexdump);
            string[] lines = hexdump.Split(newLine);

            byte[] bytes = new byte[(lines.Length - 3) * BytesPerLine];

            int done = 0;
            Parallel.For(0, lines.Length - 3, new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, i =>
            {
                string line = lines[i];

                int index = i * BytesPerLine;

                string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j < BytesPerLine; j++)
                    try
                    {
                        bytes[index + j] = Convert.ToByte(split[j + 1], 16);
                    }
                    catch
                    {
                        break;
                    }

                done++;
            });

            List<byte> end = new List<byte>();
            for (int i = lines.Length - 3; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrEmpty(line)) continue;

                string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (split.Length < 3) continue;

                for (int j = 1; j < split.Length - 1; j++)
                    try
                    {
                        end.Add(Convert.ToByte(split[j], 16));
                    }
                    catch
                    {
                        break;
                    }
            }

            if (end.Count == 0) return bytes;

            int ogLength = bytes.Length;
            Array.Resize(ref bytes, bytes.Length + end.Count);
            for (int i = 0; i < end.Count; i++)
                bytes[i + ogLength] = end[i];

            return bytes;
        }
    }
}
