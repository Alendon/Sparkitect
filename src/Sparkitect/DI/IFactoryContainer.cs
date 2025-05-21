using OneOf;
using Sparkitect.Modding;

namespace Sparkitect.DI;

public interface IFactoryContainer<TBaseType> where TBaseType : class
{
    TBaseType Resolve(OneOf<string, Identification, object> key);
}