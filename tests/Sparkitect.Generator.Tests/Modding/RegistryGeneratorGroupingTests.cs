using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.Tests.Modding;

public class RegistryGeneratorGroupingTests
{
    [Test]
    public async Task GroupUnitsByRegistry_Providers_MergesAndSorts()
    {
        var model = new RegistryModel("Reg", "cat", "NS", false, ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string, bool, bool)>());

        var u1 = new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From<RegistrationEntry>(new MethodRegistrationEntry("b", ImmutableValueArray.From<(string,string)>( ("f2","b.txt") ), "Register", "NS.Prov.M", [])));
        var u2 = new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From<RegistrationEntry>(new MethodRegistrationEntry("a", ImmutableValueArray.From<(string,string)>( ("f1","a.txt") ), "Register", "NS.Prov.M", [])));

        var grouped = RegistryGenerator.GroupUnitsByRegistry([u1, u2], SourceKind.Provider, "Providers");
        await Assert.That(grouped.Length).IsEqualTo(1);
        var unit = grouped[0];
        await Assert.That(unit.Entries.Count).IsEqualTo(2);
        await Assert.That(unit.Entries.First().Id).IsEqualTo("a");
        await Assert.That(unit.Entries.Last().Id).IsEqualTo("b");
    }

    [Test]
    public async Task GroupUnitsByRegistry_Resources_KindFiltered()
    {
        var model = new RegistryModel("Reg2", "cat2", "NS2", false, ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string, bool, bool)>());

        var prov = new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From<RegistrationEntry>(new MethodRegistrationEntry("x", ImmutableValueArray.From<(string,string)>(), "Register", "NS2.P.M", [])));
        var res = new RegistrationUnit(model, SourceKind.Yaml, "Resources",
            ImmutableValueArray.From<RegistrationEntry>(new ResourceRegistrationEntry("y", ImmutableValueArray.From<(string,string)>(), "RegRes")));

        var grouped = RegistryGenerator.GroupUnitsByRegistry(ImmutableArray.Create(prov, res), SourceKind.Yaml, "Resources");
        await Assert.That(grouped.Length).IsEqualTo(1);
        await Assert.That(grouped[0].Entries.Single().Id).IsEqualTo("y");
    }
}

