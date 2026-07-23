using Nager.FileCompressService.Jobs;
using Nager.FileCompressService.Models;
using Nager.FileCompressService.Optimizer;
using Nager.FileCompressService.Services;
using Quartz;
using Serilog;

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
    string deleteInfo = "No (Keep original files)";

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

        if (opt.DeleteOriginal)
        {
            deleteInfo = "YES (Original files will be DELETED after processing!)";
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
        " [Purge Originals]        {DeleteOriginal}\n" +
        "======================================================================",
        options.AnalyzeOnly ? "ANALYZE ONLY (Dry Run - No files will be modified)" : "PRODUCTION (Files will be optimized)",
        string.IsNullOrEmpty(options.SourceDirectory) ? "NOT SET (Service might fail)" : options.SourceDirectory,
        actualParallelism,
        extensionsInfo,
        qualityInfo,
        formatInfo,
        deleteInfo
    );
    // --- CONFIGURATION LOGGING END ---

    host.Run();
}
finally
{
    Log.CloseAndFlush();
}
