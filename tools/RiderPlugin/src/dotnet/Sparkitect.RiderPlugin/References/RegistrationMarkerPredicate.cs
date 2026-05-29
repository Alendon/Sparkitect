using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>Recognises registration attributes by their <c>IRegisterMarker</c> super-interface.</summary>
public static class RegistrationMarkerPredicate
{
    private const string RegisterMarkerFullName = "Sparkitect.Modding.IRegisterMarker";

    /// <summary>True when the attribute type implements <c>Sparkitect.Modding.IRegisterMarker</c>.</summary>
    public static bool IsRegistrationAttribute(ITypeElement? attributeType)
    {
        if (attributeType == null)
            return false;

        return attributeType.GetAllSuperTypes()
            .Any(t => t.GetClrName().FullName == RegisterMarkerFullName);
    }
}
