//HintName: ValidationTest_TestClass_LogEnricher.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace ValidationTest.LogEnricher;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
file class ValidationTest_TestClass_LogEnricher
{
    
    
    
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "zhk9L7bx60cT7P+epsO2e4cAAABUZXN0Q2xhc3MuY3M=")]    
    public static void LogIntercept_1(string messageTemplate,int propertyValue)
    {
        using var modNameContext = global::Serilog.Context.LogContext.PushProperty("ModName", "ValidationTestMod");
        using var classNameContext = global::Serilog.Context.LogContext.PushProperty("ClassName", "TestClass");
        
        Serilog.Log.Information(messageTemplate,propertyValue);
    }
    
}