using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Sparkitect.Generator.Stateless;

namespace Sparkitect.Generator.Tests.Stateless;

public class StatelessExtractionTests : SourceGeneratorTestBase<StatelessFunctionGenerator>
{
    [Before(Test)]
    public void Setup()
    {
        ReferenceAssemblies = ReferenceAssemblies.WithPackages([new PackageIdentity("OneOf", "3.0.271")]);

        TestSources.Add(TestData.GlobalUsings);
        TestSources.Add(TestData.DiAttributes);
        TestSources.Add(TestData.Sparkitect);
        TestSources.Add(TestData.ModdingCode);
        TestSources.Add(TestData.StatelessCoreTypes);
        TestSources.Add(TestData.StatelessTestTypes);

        AnalyzerConfigFiles.Add(("/TestConfig.editorconfig",
            """
            is_global = true
            build_property.ModName = Test Mod
            build_property.ModId = test_mod
            build_property.RootNamespace = TestMod
            build_property.SgOutputNamespace = TestMod.Generated
            """));
    }

    #region GetNonGenericBaseTypeName Tests

    [Test]
    public async Task GetNonGenericBaseTypeName_SimpleType_ReturnsFullName(CancellationToken token)
    {
        TestSources.Add(("TypeTest.cs",
            """
            namespace TypeTest;

            public class SimpleClass { }
            public class Container
            {
                public SimpleClass Field;
                public string StringField;
                public int IntField;
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var containerType = compilation.GetTypeByMetadataName("TypeTest.Container");
        await Assert.That(containerType).IsNotNull();

        var simpleClassField = containerType!.GetMembers("Field").OfType<IFieldSymbol>().First();
        var stringField = containerType.GetMembers("StringField").OfType<IFieldSymbol>().First();
        var intField = containerType.GetMembers("IntField").OfType<IFieldSymbol>().First();

        var simpleResult = StatelessFunctionGenerator.GetNonGenericBaseTypeName(simpleClassField.Type);
        var stringResult = StatelessFunctionGenerator.GetNonGenericBaseTypeName(stringField.Type);
        var intResult = StatelessFunctionGenerator.GetNonGenericBaseTypeName(intField.Type);

        await Assert.That(simpleResult).IsEqualTo("TypeTest.SimpleClass");
        await Assert.That(stringResult).IsEqualTo("string");
        await Assert.That(intResult).IsEqualTo("int");
    }

    [Test]
    public async Task GetNonGenericBaseTypeName_GenericType_ReturnsConstructedFromBase(CancellationToken token)
    {
        TestSources.Add(("GenericTypeTest.cs",
            """
            namespace TypeTest;

            public class Container
            {
                public List<string> ListField;
                public Dictionary<string, int> DictField;
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var containerType = compilation.GetTypeByMetadataName("TypeTest.Container");
        await Assert.That(containerType).IsNotNull();

        var listField = containerType!.GetMembers("ListField").OfType<IFieldSymbol>().First();
        var dictField = containerType.GetMembers("DictField").OfType<IFieldSymbol>().First();

        var listResult = StatelessFunctionGenerator.GetNonGenericBaseTypeName(listField.Type);
        var dictResult = StatelessFunctionGenerator.GetNonGenericBaseTypeName(dictField.Type);

        await Assert.That(listResult).IsEqualTo("System.Collections.Generic.List");
        await Assert.That(dictResult).IsEqualTo("System.Collections.Generic.Dictionary");
    }

    [Test]
    public async Task GetNonGenericBaseTypeName_NullableReferenceType_UnwrapsNullable(CancellationToken token)
    {
        TestSources.Add(("NullableTypeTest.cs",
            """
            #nullable enable
            namespace TypeTest;

            public class Container
            {
                public IDisposable? NullableDisposable;
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var containerType = compilation.GetTypeByMetadataName("TypeTest.Container");
        await Assert.That(containerType).IsNotNull();

        var nullableField = containerType!.GetMembers("NullableDisposable").OfType<IFieldSymbol>().First();
        var result = StatelessFunctionGenerator.GetNonGenericBaseTypeName(nullableField.Type);

        await Assert.That(result).IsEqualTo("System.IDisposable");
    }

    [Test]
    public async Task GetNonGenericBaseTypeName_NullType_ReturnsEmptyString(CancellationToken token)
    {
        var result = StatelessFunctionGenerator.GetNonGenericBaseTypeName(null);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    #endregion

    #region FormatTypedConstant Tests

    [Test]
    public async Task FormatTypedConstant_StringValue_ReturnsQuotedString(CancellationToken token)
    {
        TestSources.Add(("AttrTest.cs",
            """
            namespace AttrTest;

            [AttributeUsage(AttributeTargets.Class)]
            public class TestAttr : Attribute
            {
                public TestAttr(string value) { }
            }

            [TestAttr("hello")]
            public class TestClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testClass = compilation.GetTypeByMetadataName("AttrTest.TestClass");
        await Assert.That(testClass).IsNotNull();

        var attr = testClass!.GetAttributes().First();
        var typedConstant = attr.ConstructorArguments[0];

        var result = StatelessFunctionGenerator.FormatTypedConstant(typedConstant);
        await Assert.That(result).IsEqualTo("\"hello\"");
    }

    [Test]
    public async Task FormatTypedConstant_IntValue_ReturnsLiteral(CancellationToken token)
    {
        TestSources.Add(("AttrTest.cs",
            """
            namespace AttrTest;

            [AttributeUsage(AttributeTargets.Class)]
            public class TestAttr : Attribute
            {
                public TestAttr(int value) { }
            }

            [TestAttr(42)]
            public class TestClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testClass = compilation.GetTypeByMetadataName("AttrTest.TestClass");
        await Assert.That(testClass).IsNotNull();

        var attr = testClass!.GetAttributes().First();
        var typedConstant = attr.ConstructorArguments[0];

        var result = StatelessFunctionGenerator.FormatTypedConstant(typedConstant);
        await Assert.That(result).IsEqualTo("42");
    }

    [Test]
    public async Task FormatTypedConstant_BoolTrue_ReturnsTrueLiteral(CancellationToken token)
    {
        TestSources.Add(("AttrTest.cs",
            """
            namespace AttrTest;

            [AttributeUsage(AttributeTargets.Class)]
            public class TestAttr : Attribute
            {
                public TestAttr(bool value) { }
            }

            [TestAttr(true)]
            public class TestClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testClass = compilation.GetTypeByMetadataName("AttrTest.TestClass");
        await Assert.That(testClass).IsNotNull();

        var attr = testClass!.GetAttributes().First();
        var typedConstant = attr.ConstructorArguments[0];

        var result = StatelessFunctionGenerator.FormatTypedConstant(typedConstant);
        await Assert.That(result).IsEqualTo("true");
    }

    [Test]
    public async Task FormatTypedConstant_BoolFalse_ReturnsFalseLiteral(CancellationToken token)
    {
        TestSources.Add(("AttrTest.cs",
            """
            namespace AttrTest;

            [AttributeUsage(AttributeTargets.Class)]
            public class TestAttr : Attribute
            {
                public TestAttr(bool value) { }
            }

            [TestAttr(false)]
            public class TestClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testClass = compilation.GetTypeByMetadataName("AttrTest.TestClass");
        await Assert.That(testClass).IsNotNull();

        var attr = testClass!.GetAttributes().First();
        var typedConstant = attr.ConstructorArguments[0];

        var result = StatelessFunctionGenerator.FormatTypedConstant(typedConstant);
        await Assert.That(result).IsEqualTo("false");
    }

    [Test]
    public async Task FormatTypedConstant_NullValue_ReturnsNull(CancellationToken token)
    {
        TestSources.Add(("AttrTest.cs",
            """
            namespace AttrTest;

            [AttributeUsage(AttributeTargets.Class)]
            public class TestAttr : Attribute
            {
                public TestAttr(string? value) { }
            }

            [TestAttr(null)]
            public class TestClass { }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testClass = compilation.GetTypeByMetadataName("AttrTest.TestClass");
        await Assert.That(testClass).IsNotNull();

        var attr = testClass!.GetAttributes().First();
        var typedConstant = attr.ConstructorArguments[0];

        var result = StatelessFunctionGenerator.FormatTypedConstant(typedConstant);
        await Assert.That(result).IsEqualTo("null");
    }

    #endregion

    #region ExtractSchedulingParams Tests

    [Test]
    public async Task ExtractSchedulingParams_NoOrderingAttributes_ReturnsEmptyInstances(CancellationToken token)
    {
        TestSources.Add(("SchedulingTest.cs",
            """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using StatelessTest;

            namespace SchedulingTest;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("simple")]
                [TestScheduling]
                public static void SimpleMethod() { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testModule = compilation.GetTypeByMetadataName("SchedulingTest.TestModule");
        await Assert.That(testModule).IsNotNull();

        var method = testModule!.GetMembers("SimpleMethod").OfType<IMethodSymbol>().First();
        var schedulingType = compilation.GetTypeByMetadataName("StatelessTest.TestScheduling");
        await Assert.That(schedulingType).IsNotNull();

        var result = StatelessFunctionGenerator.ExtractSchedulingParams(schedulingType!, method);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Instances.Count).IsEqualTo(0);
        await Assert.That(result[1].Instances.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExtractSchedulingParams_SingleOrderAfter_ExtractsGenericArg(CancellationToken token)
    {
        TestSources.Add(("SchedulingTest.cs",
            """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using StatelessTest;

            namespace SchedulingTest;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("first")]
                [TestScheduling]
                public static void FirstMethod() { }

                [TestFunction("second")]
                [TestScheduling]
                [OrderAfter<TestModule.FirstFunc>]
                public static void SecondMethod() { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testModule = compilation.GetTypeByMetadataName("SchedulingTest.TestModule");
        await Assert.That(testModule).IsNotNull();

        var method = testModule!.GetMembers("SecondMethod").OfType<IMethodSymbol>().First();
        var schedulingType = compilation.GetTypeByMetadataName("StatelessTest.TestScheduling");
        await Assert.That(schedulingType).IsNotNull();

        var result = StatelessFunctionGenerator.ExtractSchedulingParams(schedulingType!, method);

        var orderAfterParam = result.FirstOrDefault(p => p.AttributeTypeName.Contains("OrderAfter"));
        await Assert.That(orderAfterParam).IsNotNull();
        await Assert.That(orderAfterParam!.Instances.Count).IsEqualTo(1);
        await Assert.That(orderAfterParam.Instances[0].GenericArgs.Count).IsEqualTo(1);
        await Assert.That(orderAfterParam.Instances[0].GenericArgs[0]).Contains("TestModule.FirstFunc");
    }

    [Test]
    public async Task ExtractSchedulingParams_MultipleOrderAfter_ExtractsAll(CancellationToken token)
    {
        TestSources.Add(("SchedulingTest.cs",
            """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using StatelessTest;

            namespace SchedulingTest;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("first")]
                [TestScheduling]
                public static void FirstMethod() { }

                [TestFunction("second")]
                [TestScheduling]
                public static void SecondMethod() { }

                [TestFunction("third")]
                [TestScheduling]
                [OrderAfter<TestModule.FirstFunc>]
                [OrderAfter<TestModule.SecondFunc>]
                public static void ThirdMethod() { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testModule = compilation.GetTypeByMetadataName("SchedulingTest.TestModule");
        await Assert.That(testModule).IsNotNull();

        var method = testModule!.GetMembers("ThirdMethod").OfType<IMethodSymbol>().First();
        var schedulingType = compilation.GetTypeByMetadataName("StatelessTest.TestScheduling");
        await Assert.That(schedulingType).IsNotNull();

        var result = StatelessFunctionGenerator.ExtractSchedulingParams(schedulingType!, method);

        var orderAfterParam = result.FirstOrDefault(p => p.AttributeTypeName.Contains("OrderAfter"));
        await Assert.That(orderAfterParam).IsNotNull();
        await Assert.That(orderAfterParam!.Instances.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExtractSchedulingParams_MixedOrdering_ExtractsBothTypes(CancellationToken token)
    {
        TestSources.Add(("SchedulingTest.cs",
            """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using StatelessTest;

            namespace SchedulingTest;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("first")]
                [TestScheduling]
                public static void FirstMethod() { }

                [TestFunction("third")]
                [TestScheduling]
                public static void ThirdMethod() { }

                [TestFunction("middle")]
                [TestScheduling]
                [OrderAfter<TestModule.FirstFunc>]
                [OrderBefore<TestModule.ThirdFunc>]
                public static void MiddleMethod() { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testModule = compilation.GetTypeByMetadataName("SchedulingTest.TestModule");
        await Assert.That(testModule).IsNotNull();

        var method = testModule!.GetMembers("MiddleMethod").OfType<IMethodSymbol>().First();
        var schedulingType = compilation.GetTypeByMetadataName("StatelessTest.TestScheduling");
        await Assert.That(schedulingType).IsNotNull();

        var result = StatelessFunctionGenerator.ExtractSchedulingParams(schedulingType!, method);

        var orderAfterParam = result.FirstOrDefault(p => p.AttributeTypeName.Contains("OrderAfter"));
        var orderBeforeParam = result.FirstOrDefault(p => p.AttributeTypeName.Contains("OrderBefore"));

        await Assert.That(orderAfterParam).IsNotNull();
        await Assert.That(orderBeforeParam).IsNotNull();
        await Assert.That(orderAfterParam!.Instances.Count).IsEqualTo(1);
        await Assert.That(orderBeforeParam!.Instances.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExtractSchedulingParams_OrderAfterWithErrorType_DeducesCorrectName(CancellationToken token)
    {
        TestSources.Add(("SchedulingTest.cs",
            """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;
            using StatelessTest;

            namespace SchedulingTest;

            public partial class TestModule : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;

                [TestFunction("init")]
                [TestScheduling]
                public static void InitMethod() { }

                [TestFunction("update")]
                [TestScheduling]
                [OrderAfter<InitFunc>]
                public static void UpdateMethod() { }
            }
            """));

        var (_, compilation) = await GetInitialCompilationAsync(token);

        var testModule = compilation.GetTypeByMetadataName("SchedulingTest.TestModule");
        await Assert.That(testModule).IsNotNull();

        var method = testModule!.GetMembers("UpdateMethod").OfType<IMethodSymbol>().First();
        var schedulingType = compilation.GetTypeByMetadataName("StatelessTest.TestScheduling");
        await Assert.That(schedulingType).IsNotNull();

        var result = StatelessFunctionGenerator.ExtractSchedulingParams(schedulingType!, method);

        var orderAfterParam = result.FirstOrDefault(p => p.AttributeTypeName.Contains("OrderAfter"));
        await Assert.That(orderAfterParam).IsNotNull();
        await Assert.That(orderAfterParam!.Instances.Count).IsEqualTo(1);

        var genericArg = orderAfterParam.Instances[0].GenericArgs[0];
        await Assert.That(genericArg).Contains("TestModule.InitFunc");
    }

    #endregion
}
