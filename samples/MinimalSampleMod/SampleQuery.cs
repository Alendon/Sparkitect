using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.ECS.Queries;

namespace MinimalSampleMod;

[ComponentQuery]
[WriteComponents<MinimalComponent>]
[AllowConcreteResolution]
partial class SampleQuery;
