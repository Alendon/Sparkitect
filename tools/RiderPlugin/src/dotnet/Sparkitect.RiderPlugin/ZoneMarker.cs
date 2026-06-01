using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Yaml;
using JetBrains.ReSharper.Resources.Shell;

namespace Sparkitect.RiderPlugin;

// The plugin attaches references, highlighting, a daemon stage, and a search-domain factory to
// .sparkres.yaml files, so it must require the YAML language zone (JetBrains.ReSharper.Plugins.Yaml).
// Without it the YAML-targeting solution components — the reference-provider factory and the
// search-domain factory that makes resource scalars findable — are silently dropped from activation,
// leaving the C# navigation working while YAML navigation and Find Usages produce nothing.
[ZoneMarker]
public class ZoneMarker : IRequire<PsiFeaturesImplZone>, IRequire<ILanguageYamlZone>;
