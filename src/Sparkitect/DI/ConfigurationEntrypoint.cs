namespace Sparkitect.DI;

public interface BaseConfigurationEntrypoint
{
    public static abstract Type EntrypointFactoryAttributeType { get; }
}

public interface ConfigurationEntrypoint<TDiscoveryAttribute> : BaseConfigurationEntrypoint where TDiscoveryAttribute : Attribute 
{
    static Type BaseConfigurationEntrypoint.EntrypointFactoryAttributeType => typeof(TDiscoveryAttribute);
}