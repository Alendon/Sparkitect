using Serilog;
using Serilog.Events;

namespace LogEnricherExample;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog with the same template as in Sparkitect
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}][{ModName}/{Class}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // Log messages - these will be intercepted and enriched with ModName and Class properties
        Log.Information("Starting sample application");
        Log.Debug("This is a debug message with a value: {Value}", 42);
        
        Log.Warning("This is a warning message");
        
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while processing");
        }
        
        // Log with explicit level
        Log.Write(LogEventLevel.Information, "Log message with explicit level {Value}", 100);
        
        // Log in a different class
        var otherLogger = new OtherLogger();
        otherLogger.LogSomething();
        
        // Demonstrate nested context
        using (Serilog.Context.LogContext.PushProperty("CustomProperty", "CustomValue"))
        {
            Log.Information("Log with custom property should have both ModName/Class and CustomProperty");
        }

        Log.Information("Shutting down sample application");
        Log.CloseAndFlush();
    }
}

public class OtherLogger
{
    public void LogSomething()
    {
        // This log will be intercepted and use OtherLogger as the Class value
        Log.Information("Logging from another class");
    }
}
