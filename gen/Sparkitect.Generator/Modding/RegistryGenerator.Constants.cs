namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    private const string RegistryMarkerAttribute = "Sparkitect.Modding.RegistryAttribute";
    private const string RegistryMethodMarkerAttribute = "Sparkitect.Modding.RegistryMethodAttribute";
    private const string UseResourceFileAttribute = "Sparkitect.Modding.UseResourceFileAttribute";
    
    private const string RegistryInterface = "Sparkitect.Modding.IRegistry";
    private const string RegistryAttributeIdField = "Identifier";
    private const string IdentificationStruct = "Sparkitect.Modding.Identification";
    
    
    private const string RegistryMetadataAttribute = "Sparkitect.Modding.RegistryMetadataAttribute";

    private const string ResourceFileSuffix = ".sparkres.yaml";
}