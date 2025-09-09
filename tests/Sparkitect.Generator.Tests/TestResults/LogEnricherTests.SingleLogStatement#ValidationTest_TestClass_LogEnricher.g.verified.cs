//HintName: ValidationTest_TestClass_LogEnricher.g.cs
namespace ValidationTest.LogEnricher;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
file class ValidationTest_TestClass_LogEnricher
{
    
    
    
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "0yU4S3hyw6+uLUK+5+US4o8AAABUZXN0Q2xhc3MuY3M=")]    
    public static void LogIntercept_1(string messageTemplate,int propertyValue)
    {
        using var modNameContext = global::Serilog.Context.LogContext.PushProperty("ModName", "ValidationTestMod");
        using var classNameContext = global::Serilog.Context.LogContext.PushProperty("ClassName", "TestClass");
        
        Serilog.Log.Information(messageTemplate,propertyValue);
    }
    
}