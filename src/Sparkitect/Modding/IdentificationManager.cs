using Sparkitect.GameState;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;

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
        if (_categoryIds.ContainsKey(categoryId))
        {
            new InvalidOperationException($"Category '{categoryId}' is already registered. Duplicate category registration is not allowed.").Throw();
        }

        if (_categoryIds.Count >= ushort.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register category '{categoryId}': Maximum number of categories ({ushort.MaxValue}) reached.");
        }

        ushort category = (ushort)(_categoryIds.Count + 1);
        _categoryIds.Add(categoryId, category);
        return category;
    }

    // Read paths preserve pre-reshape thread-affinity (no AssertMainThread): the previous
    // TryGet* lookups did not assert and IdentificationDebuggerProxy invokes these from
    // arbitrary threads under the debugger.
    public Result<ushort, ResolveError> GetModId(string modName)
    {
        if (_modIds.TryGetValue(modName, out var id))
            return id;
        return new ResolveError.UnknownMod(modName);
    }

    public Result<ushort, ResolveError> GetCategoryId(string categoryName)
    {
        if (_categoryIds.TryGetValue(categoryName, out var id))
            return id;
        return new ResolveError.UnknownCategory(categoryName);
    }

    public Result<string, ResolveError> GetModId(ushort modId)
    {
        if (_modIds.Inverse.TryGetValue(modId, out var name))
            return name;
        return new ResolveError.UnknownMod(modId);
    }

    public Result<string, ResolveError> GetCategoryId(ushort categoryId)
    {
        if (_categoryIds.Inverse.TryGetValue(categoryId, out var name))
            return name;
        return new ResolveError.UnknownCategory(categoryId);
    }

    public Identification RegisterObject(Variant<string, ushort> modId, Variant<string, ushort> categoryId, string objectId)
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

    public Result<Identification, ResolveError> GetObjectId(
        Variant<string, ushort> modId,
        Variant<string, ushort> categoryId,
        Variant<string, ushort> objectId)
    {
        // Fail-fast order: mod → category → object.
        // Pre-reshape TryGetObjectId did not assert main thread; preserved here.
        var resolvedModId = ResolveModId(modId);
        if (resolvedModId == 0)
            return new ResolveError.UnknownMod(modId);

        var resolvedCategoryId = ResolveCategoryId(categoryId);
        if (resolvedCategoryId == 0)
            return new ResolveError.UnknownCategory(categoryId);

        var key = (resolvedModId, resolvedCategoryId);
        if (!_objectIds.TryGetValue(key, out var idDictionary))
            return new ResolveError.UnknownObject(objectId);

        return objectId switch
        {
            Variant<string, ushort>.Of1 strId
                => idDictionary.TryGetValue(strId.Value, out var v)
                    ? Identification.Create(resolvedModId, resolvedCategoryId, v)
                    : (Result<Identification, ResolveError>)new ResolveError.UnknownObject(objectId),
            Variant<string, ushort>.Of2 numId
                => idDictionary.Inverse.ContainsKey(numId.Value)
                    ? Identification.Create(resolvedModId, resolvedCategoryId, numId.Value)
                    : (Result<Identification, ResolveError>)new ResolveError.UnknownObject(objectId),
        };
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

    public IEnumerable<Identification> GetAllObjectIdsOfMod(Variant<string, ushort> modId)
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

    public IEnumerable<Identification> GetAllObjectIdsOfCategory(Variant<string, ushort> categoryId)
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

    public IEnumerable<Identification> GetAllObjectIdsOfModAndCategory(Variant<string, ushort> modId, Variant<string, ushort> categoryId)
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

        var removed = idDict.Remove(objectIdString);

        // Prune the emptied (mod, category) bucket so it stops reading as a live dependent.
        // UnregisterCategory/UnregisterMod gate on key presence in _objectIds, so a lingering
        // empty bucket blocks symmetric category/mod teardown (F-02 shader_module teardown).
        if (removed && idDict.Count == 0)
            _objectIds.Remove(key);

        return removed;
    }

    public IEnumerable<ushort> GetRegisteredMods()
    {
        return _modIds.Values;
    }

    public IEnumerable<ushort> GetRegisteredCategories()
    {
        return _categoryIds.Values;
    }

    public bool IsModRegistered(Variant<string, ushort> modId)
    {
        ushort resolvedModId = ResolveModId(modId);
        return resolvedModId != 0;
    }

    public bool IsCategoryRegistered(Variant<string, ushort> categoryId)
    {
        ushort resolvedCategoryId = ResolveCategoryId(categoryId);
        return resolvedCategoryId != 0;
    }

    public bool IsObjectRegistered(Variant<string, ushort> modId, Variant<string, ushort> categoryId, Variant<string, ushort> objectId)
    {
        return GetObjectId(modId, categoryId, objectId) is Result<Identification, ResolveError>.Ok;
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

    public int GetObjectCountForCategory(Variant<string, ushort> modId, Variant<string, ushort> categoryId)
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

    // Internal helpers stay sentinel-shaped: 0 on miss.
    private ushort ResolveModId(Variant<string, ushort> modId) => modId switch
    {
        Variant<string, ushort>.Of1 strId => _modIds.TryGetValue(strId.Value, out var resolved) ? resolved : (ushort)0,
        Variant<string, ushort>.Of2 numericId => _modIds.Inverse.ContainsKey(numericId.Value) ? numericId.Value : (ushort)0,
    };

    private ushort ResolveCategoryId(Variant<string, ushort> categoryId) => categoryId switch
    {
        Variant<string, ushort>.Of1 strId => _categoryIds.TryGetValue(strId.Value, out var resolved) ? resolved : (ushort)0,
        Variant<string, ushort>.Of2 numericId => _categoryIds.Inverse.ContainsKey(numericId.Value) ? numericId.Value : (ushort)0,
    };

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