using System;

namespace MCEPatcher.Core;

public static class ConsoleHelper
{
    public static void PAKE()
    {
        if (ApkProcessor.NonInteractive)
        {
            return;
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(true);
    }
}
