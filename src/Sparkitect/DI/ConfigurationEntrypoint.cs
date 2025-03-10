namespace Sparkitect.DI;

public interface BaseConfigurationEntrypoint
{
    public static abstract Type EntrypointAttributeType { get; }
}

public interface ConfigurationEntrypoint<TDiscoveryAttribute> : BaseConfigurationEntrypoint where TDiscoveryAttribute : Attribute 
{
    static Type BaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}