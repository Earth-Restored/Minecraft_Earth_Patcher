using CommandLine;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog;

namespace MCEPatcher.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/debug.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 8338607, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Debug()
                .CreateLogger();
            Log.Logger = log;

            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                Log.Fatal($"Unhandled exception: {e.ExceptionObject}");
                Log.CloseAndFlush();
                U.PAKE();
                Environment.Exit(1);
            };

            ParserResult<ApkProcessor.Options> res = Parser.Default.ParseArguments<ApkProcessor.Options>(args);

            ApkProcessor.Options options;
            if (res is Parsed<ApkProcessor.Options> parsed)
                options = parsed.Value;
            else if (res is NotParsed<ApkProcessor.Options> notParsed)
            {
                if (res.Errors.Any(error => error is HelpRequestedError))
                    return;
                else if (res.Errors.Any(error => error is VersionRequestedError))
                    return;
                else
                {
                    Environment.Exit(2);
                    return;
                }
            }
            else
            {
                Environment.Exit(2);
                return;
            }

            try
            {
                if (!ApkProcessor.Run(options))
                {
                    U.PAKE();
                    Environment.Exit(1);
                }
                else
                    U.PAKE();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.ToString());
                U.PAKE();
                Environment.Exit(1);
            }
        }
    }
}
