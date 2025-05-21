namespace Sparkitect.DI.Models;

internal class PropertyDependency
{
    public Type DependencyType { get; }
    public string PropertyName { get; }
    public bool IsOptional { get; }
    
    public PropertyDependency(Type dependencyType, string propertyName, bool isOptional)
    {
        DependencyType = dependencyType;
        PropertyName = propertyName;
        IsOptional = isOptional;
    }
}