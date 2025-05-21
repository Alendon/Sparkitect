using OneOf;
using Sparkitect.Modding;

namespace Sparkitect.DI;

internal sealed class FactoryContainer<TBaseType> : IFactoryContainer<TBaseType> where TBaseType : class
{
    public TBaseType Resolve(OneOf<string, Identification, object> key)
    {
        throw new NotImplementedException();
    }
}