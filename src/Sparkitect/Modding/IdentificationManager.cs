using OneOf;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

[CreateServiceFactory<IIdentificationManager>]
internal class IdentificationManager : IIdentificationManager
{
    private readonly BidirectionalDictionary<string, ushort> _modIds = new();
    private readonly BidirectionalDictionary<string, ushort> _categoryIds = new();
    private readonly Dictionary<(ushort modId, ushort categoryId), BidirectionalDictionary<string, uint>> _objectIds = new();
    
    public ushort RegisterMod(string modId)
    {
        if (_modIds.TryGetValue(modId, out var mod))
        {
            return mod;
        }
        
        mod = (ushort)(_modIds.Count + 1);
        _modIds.Add(modId, mod);
        return mod;
    }

    public ushort RegisterCategory(string categoryId)
    {
        if (_categoryIds.TryGetValue(categoryId, out var category))
        {
            return category;
        }
        
        category =  (ushort)(_categoryIds.Count + 1);
        _categoryIds.Add(categoryId, category);
        return category;
    }

    public bool TryGetModId(string modId, out ushort id)
    {
        return _modIds.TryGetValue(modId, out id);
    }

    public bool TryGetCategoryId(string categoryId, out ushort id)
    {
        return _categoryIds.TryGetValue(categoryId, out id);
    }

    public bool TryGetModId(ushort id, out string modId)
    {
        return _modIds.Inverse.TryGetValue(id, out modId);
    }

    public bool TryGetCategoryId(ushort id, out string categoryId)
    {
        return _categoryIds.Inverse.TryGetValue(id, out categoryId);
    }

    public Identification RegisterObject(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId, string objectId)
    {
        ushort resolvedModId = ResolveModId(modId);
        ushort resolvedCategoryId = ResolveCategoryId(categoryId);
        
        if (resolvedModId == 0 || resolvedCategoryId == 0)
        {
            return Identification.Empty;
        }
        
        var key = (resolvedModId, resolvedCategoryId);
        
        if (!_objectIds.TryGetValue(key, out var idDictionary))
        {
            idDictionary = new BidirectionalDictionary<string, uint>();
            _objectIds[key] = idDictionary;
        }
        
        if (idDictionary.TryGetValue(objectId, out var itemId))
        {
            return Identification.Create(resolvedModId, resolvedCategoryId, itemId);
        }
        
        uint newItemId = (uint)(idDictionary.Count + 1);
        idDictionary.Add(objectId, newItemId);
        
        return Identification.Create(resolvedModId, resolvedCategoryId, newItemId);
    }

    public bool TryGetObjectId(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId, OneOf<string, ushort> objectId, out Identification id)
    {
        ushort resolvedModId = ResolveModId(modId);
        ushort resolvedCategoryId = ResolveCategoryId(categoryId);
        
        if (resolvedModId == 0 || resolvedCategoryId == 0)
        {
            id = Identification.Empty;
            return false;
        }
        
        var key = (resolvedModId, resolvedCategoryId);
        
        if (!_objectIds.TryGetValue(key, out var idDictionary))
        {
            id = Identification.Empty;
            return false;
        }
        
        Identification idResult = Identification.Empty;
        bool result = objectId.Match(
            stringId =>
            {
                if (!idDictionary.TryGetValue(stringId, out var value)) return false;
                
                idResult = Identification.Create(resolvedModId, resolvedCategoryId, value);
                return true;

            },
            numericId =>
            {
                if (!idDictionary.Inverse.ContainsKey(numericId)) return false;
                
                idResult = Identification.Create(resolvedModId, resolvedCategoryId, numericId);
                return true;

            }
        );
        
        id = idResult;
        return result;
    }

    public IEnumerable<Identification> GetAllObjectIds()
    {
        foreach (var ((modId, categoryId), idDict) in _objectIds)
        {
            foreach (var itemId in idDict.Values)
            {
                yield return Identification.Create(modId, categoryId, itemId);
            }
        }
    }

    public IEnumerable<Identification> GetAllObjectIdsOfMod(OneOf<string, ushort> modId)
    {
        ushort resolvedModId = ResolveModId(modId);
        
        if (resolvedModId == 0)
        {
            yield break;
        }
        
        foreach (var ((mId, categoryId), idDict) in _objectIds)
        {
            if (mId != resolvedModId)
            {
                continue;
            }
            
            foreach (var itemId in idDict.Values)
            {
                yield return Identification.Create(mId, categoryId, itemId);
            }
        }
    }

    public IEnumerable<Identification> GetAllObjectIdsOfCategory(OneOf<string, ushort> categoryId)
    {
        ushort resolvedCategoryId = ResolveCategoryId(categoryId);
        
        if (resolvedCategoryId == 0)
        {
            yield break;
        }
        
        foreach (var ((modId, catId), idDict) in _objectIds)
        {
            if (catId != resolvedCategoryId)
            {
                continue;
            }
            
            foreach (var itemId in idDict.Values)
            {
                yield return Identification.Create(modId, catId, itemId);
            }
        }
    }

    public IEnumerable<Identification> GetAllObjectIdsOfModAndCategory(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId)
    {
        ushort resolvedModId = ResolveModId(modId);
        ushort resolvedCategoryId = ResolveCategoryId(categoryId);
        
        if (resolvedModId == 0 || resolvedCategoryId == 0)
        {
            yield break;
        }
        
        var key = (resolvedModId, resolvedCategoryId);

        if (!_objectIds.TryGetValue(key, out var idDict)) yield break;
        
        foreach (var itemId in idDict.Values)
        {
            yield return Identification.Create(resolvedModId, resolvedCategoryId, itemId);
        }
    }

    public bool UnregisterMod(ushort modId)
    {
        if (!_modIds.Inverse.TryGetValue(modId, out var modIdString))
        {
            return false;
        }

        _modIds.Remove(modIdString);
        
        // Remove all objects belonging to this mod
        var keysToRemove = _objectIds.Keys
            .Where(key => key.modId == modId)
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _objectIds.Remove(key);
        }
        
        return true;
    }

    public bool UnregisterCategory(ushort categoryId)
    {
        if (!_categoryIds.Inverse.TryGetValue(categoryId, out var categoryIdString))
        {
            return false;
        }

        _categoryIds.Remove(categoryIdString);
        
        // Remove all objects belonging to this category
        var keysToRemove = _objectIds.Keys
            .Where(key => key.categoryId == categoryId)
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _objectIds.Remove(key);
        }
        
        return true;
    }

    public bool UnregisterObject(Identification id)
    {
        var key = (id.ModId, id.CategoryId);
        
        if (!_objectIds.TryGetValue(key, out var idDict))
        {
            return false;
        }
        
        if (!idDict.Inverse.TryGetValue(id.ItemId, out var objectIdString))
        {
            return false;
        }

        return idDict.Remove(objectIdString);
    }
    
    private ushort ResolveModId(OneOf<string, ushort> modId)
    {
        return modId.Match(
            strId => TryGetModId(strId, out var id) ? id : (ushort)0,
            id => _modIds.Inverse.ContainsKey(id) ? id : (ushort)0
        );
    }
    
    private ushort ResolveCategoryId(OneOf<string, ushort> categoryId)
    {
        return categoryId.Match(
            strId => TryGetCategoryId(strId, out var id) ? id : (ushort)0,
            id => _categoryIds.Inverse.ContainsKey(id) ? id : (ushort)0
        );
    }
}