using Nager.FileCompressService.Jobs;
using Nager.FileCompressService.Models;
using Nager.FileCompressService.Optimizer;
using Nager.FileCompressService.Services;
using Quartz;
using Serilog;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

if (args.Length > 0)
{
    if (!OperatingSystem.IsWindows())
    {
        return;
    }

    [SupportedOSPlatform("windows")]
    static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    var exePath = Process.GetCurrentProcess().MainModule!.FileName;
    var serviceName = "Nager.FileCompressService";

    static void showHelp()
    {
        Console.WriteLine("""
                ==========================================================
                Nager FileCompress Service - Command Line Setup Options
                ==========================================================

                Usage:
                  Nager.FileCompressService.exe [option]

                Options:
                  /install     Registers and starts the Windows Service.
                  /uninstall   Stops and removes the Windows Service.
                  /? , /help   Displays this help screen.

                Note: /install and /uninstall require Administrator privileges.
                """);
    }

    if (args[0].Equals("/?", StringComparison.OrdinalIgnoreCase))
    {
        showHelp();
        return;
    }
    else if (args[0].Equals("/help", StringComparison.OrdinalIgnoreCase))
    {
        showHelp();
        return;
    }
    else if (args[0].Equals("/install", StringComparison.OrdinalIgnoreCase))
    {
        if (!IsAdministrator())
        {
            Console.WriteLine("Administrator privileges are required to run this command.");
            return;
        }

        // Register and start the service
        Process.Start("sc.exe", $"create \"{serviceName}\" binPath= \"{exePath}\" start= auto")?.WaitForExit();
        Process.Start("sc.exe", $"start \"{serviceName}\"")?.WaitForExit();
        return;
    }
    else if (args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase))
    {
        if (!IsAdministrator())
        {
            Console.WriteLine("Administrator privileges are required to run this command.");
            return;
        }

        // Stop and delete the service
        Process.Start("sc.exe", $"stop \"{serviceName}\"")?.WaitForExit();
        Process.Start("sc.exe", $"delete \"{serviceName}\"")?.WaitForExit();
        return;
    }
}

try
{
    var builder = Host.CreateApplicationBuilder(args);

    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string logFilePath = Path.Combine(baseDirectory, "Logs", "log-.txt");

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console() // Optional: remove if you don't need console output
        .WriteTo.File(
            path: logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .CreateLogger();

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Nager File Compress Service";
    });

    builder.Services.Configure<FileProcessorOptions>(
        builder.Configuration.GetSection(FileProcessorOptions.SectionName)
    );

    builder.Services.AddTransient<IImageOptimizer, ImageSharpImageOptimizer>();
    //builder.Services.AddTransient<IImageOpimizer, SkiaImageOpimizer>();
    //builder.Services.AddTransient<IFileCompressionHistoryService, NtfsAdsFileCompressionHistoryService>();
    builder.Services.AddTransient<IFileCompressionHistoryService, SqliteFileCompressionHistoryService>();
    builder.Services.AddTransient<IImageCompressionService, ImageCompressionService>();

    builder.Services.AddQuartz(q =>
    {
        var jobKey = new JobKey("FileCompressJob");

        q.AddJob<FileCompressJob>(opts => opts.WithIdentity(jobKey));

        if (Environment.UserInteractive)
        {
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("FileCompressStartupTrigger")
                .StartNow());
        }

        q.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity("FileCompressTrigger")
            .WithCronSchedule("0 0 2 * * ?")); // Daily at 02:00 AM
    });

    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
    });

    var host = builder.Build();

    // --- CONFIGURATION LOGGING ---
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var options = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileProcessorOptions>>().Value;

    var actualParallelism = options.MaxDegreeOfParallelism == 0
        ? $"{Environment.ProcessorCount} (Auto-detected based on CPU cores)"
        : options.MaxDegreeOfParallelism.ToString();

    string extensionsInfo = "None configured";
    string qualityInfo = "N/A";
    string formatInfo = "Keep Original Format";
    string keepInfo = "No (Original files will be DELETED after processing!)";

    if (options.ImageOptimizer != null)
    {
        var opt = options.ImageOptimizer;

        if (opt.FileExtensions != null && opt.FileExtensions.Length > 0)
        {
            extensionsInfo = string.Join(", ", opt.FileExtensions);
        }

        qualityInfo = $"{opt.Quality}%";

        if (!string.IsNullOrWhiteSpace(opt.OutputFormat))
        {
            formatInfo = opt.OutputFormat.ToUpperInvariant();
        }

        if (opt.KeepOriginal)
        {
            keepInfo = "YES (Original files will be NOT DELETED)";
        }
    }

    logger.LogInformation(
        "\n======================================================================\n" +
        " Nager File Compress Service - Starting Up\n" +
        "======================================================================\n" +
        " [Service Mode]           {Mode}\n" +
        " [Source Directory]       {SourceDir}\n" +
        " [Max Parallelism]        {Parallelism}\n" +
        "----------------------------------------------------------------------\n" +
        " Engine Settings (ImageOptimizer):\n" +
        " [Target Extensions]      {Extensions}\n" +
        " [Compression Quality]    {Quality}\n" +
        " [Output Format]          {OutputFormat}\n" +
        " [Keep Originals]        {KeepOriginal}\n" +
        "======================================================================",
        options.AnalyzeOnly ? "ANALYZE ONLY (Dry Run - No files will be modified)" : "PRODUCTION (Files will be optimized)",
        string.IsNullOrEmpty(options.SourceDirectory) ? "NOT SET (Service might fail)" : options.SourceDirectory,
        actualParallelism,
        extensionsInfo,
        qualityInfo,
        formatInfo,
        keepInfo
    );
    // --- CONFIGURATION LOGGING END ---

    host.Run();
}
finally
{
    Log.CloseAndFlush();
}
