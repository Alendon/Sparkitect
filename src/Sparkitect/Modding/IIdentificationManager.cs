using OneOf;

namespace Sparkitect.Modding;

public interface IIdentificationManager
{
    ushort RegisterMod(string modId);
    ushort RegisterCategory(string categoryId);

    bool TryGetModId(string modId, out ushort id);
    bool TryGetCategoryId(string categoryId, out ushort id);

    bool TryGetModId(ushort id, out string modId);
    bool TryGetCategoryId(ushort id, out string categoryId);

    Identification RegisterObject(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId, string objectId);

    bool TryGetObjectId(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId, OneOf<string, ushort> objectId,
        out Identification id);
    
    
    IEnumerable<Identification> GetAllObjectIds();
    IEnumerable<Identification> GetAllObjectIdsOfMod(OneOf<string, ushort> modId);
    IEnumerable<Identification> GetAllObjectIdsOfCategory(OneOf<string, ushort> categoryId);
    IEnumerable<Identification> GetAllObjectIdsOfModAndCategory(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId);
    
    // Returns true if the object was unregistered, false if it was not found
    bool UnregisterMod(ushort modId);
    bool UnregisterCategory(ushort categoryId);
    bool UnregisterObject(Identification id);
}