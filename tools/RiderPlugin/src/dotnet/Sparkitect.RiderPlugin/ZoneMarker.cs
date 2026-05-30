using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Plugins.Yaml;
using JetBrains.ReSharper.Resources.Shell;

namespace Sparkitect.RiderPlugin;

// The plugin attaches references, highlighting, and a daemon stage to .sparkres.yaml files, so it must
// require the YAML language zone (JetBrains.ReSharper.Plugins.Yaml). Without it the YAML-targeting
// solution components — notably the reference-provider factory — are silently dropped from activation,
// leaving the C# navigation working while YAML navigation produces no references at all.
[ZoneMarker]
public class ZoneMarker : IRequire<PsiFeaturesImplZone>, IRequire<ILanguageYamlZone>;
