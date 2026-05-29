using System.Linq;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;

namespace Sparkitect.RiderPlugin.References;

/// <summary>Recognises registration attributes by the forward <c>RegistrationMarkerAttribute</c> they carry.</summary>
public static class RegistrationMarkerPredicate
{
    private const string RegistrationMarkerFullName = "Sparkitect.Modding.RegistrationMarkerAttribute";

    /// <summary>True when the attribute type itself carries <c>[RegistrationMarker(category)]</c>.</summary>
    public static bool IsRegistrationAttribute(ITypeElement? attributeType)
    {
        if (attributeType == null)
            return false;

        return attributeType
            .GetAttributeInstances(new ClrTypeName(RegistrationMarkerFullName), AttributesSource.Self)
            .Any();
    }
}
