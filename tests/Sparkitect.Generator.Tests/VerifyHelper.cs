using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyTests;

namespace Sparkitect.Generator.Tests;

public class VerifyHelper
{
    
    [ModuleInitializer]
    public static void InitVerify()
    {
        VerifySourceGenerators.Initialize();
    }
}