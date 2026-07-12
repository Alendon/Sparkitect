//HintName: ValidationTest_TestClass_LogEnricher.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace ValidationTest.Generated.LogEnricher;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
file static class ValidationTest_TestClass_LogEnricher
{
    
    
    
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "R+TrDnG3SsmLL2DaBF1IxJgAAABUZXN0Q2xhc3MuY3M=")]    
    public static void LogIntercept_1(this Serilog.ILogger __value,string messageTemplate,int propertyValue)
    {
        using var modNameContext = global::Serilog.Context.LogContext.PushProperty("ModName", "ValidationTestMod");
        using var classNameContext = global::Serilog.Context.LogContext.PushProperty("ClassName", "TestClass");
        
        __value.Information(messageTemplate,propertyValue);
    }
    
}