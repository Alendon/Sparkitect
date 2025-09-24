namespace Sparkitect.DI;

public interface IBaseConfigurationEntrypoint
{
    // The attribute that marks discovery for this configuration entrypoint type
    public static abstract Type EntrypointAttributeType { get; }
}

public interface IConfigurationEntrypoint<TDiscoveryAttribute> : IBaseConfigurationEntrypoint where TDiscoveryAttribute : Attribute 
{
    static Type IBaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}
