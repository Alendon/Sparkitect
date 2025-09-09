namespace Sparkitect.DI;

public interface BaseConfigurationEntrypoint
{
    // The attribute that marks discovery for this configuration entrypoint type
    public static abstract Type EntrypointAttributeType { get; }
}

public interface ConfigurationEntrypoint<TDiscoveryAttribute> : BaseConfigurationEntrypoint where TDiscoveryAttribute : Attribute 
{
    static Type BaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}
