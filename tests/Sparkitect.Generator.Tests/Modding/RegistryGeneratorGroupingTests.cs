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
        var model = new RegistryModel("Reg", "cat", "NS", ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string,bool)>());

        var u1 = new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From(new RegistrationEntry("b", EntryKind.Method, "Register", "NS.Prov", "M", ImmutableValueArray.From<(string,string)>( ("f2","b.txt") ), [])));
        var u2 = new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From(new RegistrationEntry("a", EntryKind.Method, "Register", "NS.Prov", "M", ImmutableValueArray.From<(string,string)>( ("f1","a.txt") ), [])));

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
        var model = new RegistryModel("Reg2", "cat2", "NS2", ImmutableValueArray.From<RegisterMethodModel>(), ImmutableValueArray.From<(string,bool)>());

        var prov = new RegistrationUnit(model, SourceKind.Provider, "Providers",
            ImmutableValueArray.From(new RegistrationEntry("x", EntryKind.Method, "Register", "NS2.P", "M", ImmutableValueArray.From<(string,string)>(), [])));
        var res = new RegistrationUnit(model, SourceKind.Yaml, "Resources",
            ImmutableValueArray.From(new RegistrationEntry("y", EntryKind.Resource, "RegRes", "", "", ImmutableValueArray.From<(string,string)>(), [])));

        var grouped = RegistryGenerator.GroupUnitsByRegistry(ImmutableArray.Create(prov, res), SourceKind.Yaml, "Resources");
        await Assert.That(grouped.Length).IsEqualTo(1);
        await Assert.That(grouped[0].Entries.Single().Id).IsEqualTo("y");
    }
}

