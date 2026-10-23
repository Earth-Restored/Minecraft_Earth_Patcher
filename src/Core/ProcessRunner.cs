using Serilog;
using System.Diagnostics;

namespace MCEPatcher.Core;

public static class ProcessRunner
{
    public static Process Run(string file, string workDir, params string[] args)
    {
        var shellExecute = false;

        var startInfo = new ProcessStartInfo()
        {
            FileName = file,
            Arguments = string.Join(' ', args),
            WorkingDirectory = workDir,
            UseShellExecute = shellExecute,
            CreateNoWindow = !shellExecute,
            RedirectStandardOutput = !shellExecute,
            RedirectStandardError = !shellExecute,
            RedirectStandardInput = !shellExecute,
        };
        var process = new Process()
        {
            StartInfo = startInfo,
        };

        if (!shellExecute)
        {
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Debug("[OUT] " + e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Error("[ERR] " + e.Data);
                }
            };
        }

        process.Start();
        if (!shellExecute)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        return process;
    }
}
