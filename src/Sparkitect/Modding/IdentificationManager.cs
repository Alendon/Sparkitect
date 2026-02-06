using OneOf;
using Sparkitect.GameState;

namespace Sparkitect.Modding;

[StateService<IIdentificationManager, CoreModule>]
internal class IdentificationManager : IIdentificationManager
{
    private readonly int _mainThreadId = Environment.CurrentManagedThreadId;
    private readonly BidirectionalDictionary<string, ushort> _modIds = new();
    private readonly BidirectionalDictionary<string, ushort> _categoryIds = new();
    private readonly Dictionary<(ushort modId, ushort categoryId), BidirectionalDictionary<string, uint>> _objectIds = new();

    private void AssertMainThread([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        if (Environment.CurrentManagedThreadId != _mainThreadId)
            throw new InvalidOperationException(
                $"{caller} must be called from the main thread");
    }
    
    public ushort RegisterMod(string modId)
    {
        AssertMainThread();
        if (_modIds.TryGetValue(modId, out var mod))
        {
            return mod;
        }

        if (_modIds.Count >= ushort.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register mod '{modId}': Maximum number of mods ({ushort.MaxValue}) reached.");
        }

        mod = (ushort)(_modIds.Count + 1);
        _modIds.Add(modId, mod);
        return mod;
    }

    public ushort RegisterCategory(string categoryId)
    {
        AssertMainThread();
        if (_categoryIds.TryGetValue(categoryId, out var category))
        {
            return category;
        }

        if (_categoryIds.Count >= ushort.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register category '{categoryId}': Maximum number of categories ({ushort.MaxValue}) reached.");
        }

        category = (ushort)(_categoryIds.Count + 1);
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
        AssertMainThread();
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

        // Note: Dictionary.Count is int, so this check is theoretical (int.MaxValue < uint.MaxValue)
        // but kept for clarity about the Identification itemId field's uint capacity
#pragma warning disable CS0652 // Comparison to integral constant is useless
        if (idDictionary.Count >= uint.MaxValue)
#pragma warning restore CS0652
        {
            throw new InvalidOperationException($"Cannot register object '{objectId}': Maximum number of objects ({uint.MaxValue}) reached for mod:category combination.");
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
        AssertMainThread();
        if (!_modIds.Inverse.TryGetValue(modId, out var modIdString))
        {
            return false;
        }

        var hasDependents = _objectIds.Keys.Any(key => key.modId == modId);
        if (hasDependents)
        {
            return false;
        }

        _modIds.Remove(modIdString);
        return true;
    }

    public bool UnregisterCategory(ushort categoryId)
    {
        AssertMainThread();
        if (!_categoryIds.Inverse.TryGetValue(categoryId, out var categoryIdString))
        {
            return false;
        }

        var hasDependents = _objectIds.Keys.Any(key => key.categoryId == categoryId);
        if (hasDependents)
        {
            return false;
        }

        _categoryIds.Remove(categoryIdString);
        return true;
    }

    public bool UnregisterObject(Identification id)
    {
        AssertMainThread();
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

    public IEnumerable<ushort> GetRegisteredMods()
    {
        return _modIds.Values;
    }

    public IEnumerable<ushort> GetRegisteredCategories()
    {
        return _categoryIds.Values;
    }

    public bool IsModRegistered(OneOf<string, ushort> modId)
    {
        ushort resolvedModId = ResolveModId(modId);
        return resolvedModId != 0;
    }

    public bool IsCategoryRegistered(OneOf<string, ushort> categoryId)
    {
        ushort resolvedCategoryId = ResolveCategoryId(categoryId);
        return resolvedCategoryId != 0;
    }

    public bool IsObjectRegistered(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId, OneOf<string, ushort> objectId)
    {
        return TryGetObjectId(modId, categoryId, objectId, out _);
    }

    public int GetModCount()
    {
        return _modIds.Count;
    }

    public int GetCategoryCount()
    {
        return _categoryIds.Count;
    }

    public int GetObjectCount()
    {
        return _objectIds.Values.Sum(dict => dict.Count);
    }

    public int GetObjectCountForCategory(OneOf<string, ushort> modId, OneOf<string, ushort> categoryId)
    {
        ushort resolvedModId = ResolveModId(modId);
        ushort resolvedCategoryId = ResolveCategoryId(categoryId);

        if (resolvedModId == 0 || resolvedCategoryId == 0)
        {
            return 0;
        }

        var key = (resolvedModId, resolvedCategoryId);
        return _objectIds.TryGetValue(key, out var idDict) ? idDict.Count : 0;
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

    public bool TryResolveIdentification(Identification id, out string? modId, out string? categoryId, out string? objectId)
    {
        modId = null;
        categoryId = null;
        objectId = null;

        bool hasModId = _modIds.Inverse.TryGetValue(id.ModId, out var mod);
        bool hasCategoryId = _categoryIds.Inverse.TryGetValue(id.CategoryId, out var category);

        if (hasModId) modId = mod;
        if (hasCategoryId) categoryId = category;

        var key = (id.ModId, id.CategoryId);
        if (_objectIds.TryGetValue(key, out var idDict) && idDict.Inverse.TryGetValue(id.ItemId, out var obj))
        {
            objectId = obj;
        }

        return hasModId && hasCategoryId && objectId is not null;
    }
}