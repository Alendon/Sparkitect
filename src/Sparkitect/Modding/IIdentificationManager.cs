using Sparkitect.Utils.DU;

using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Manages the identification system for mods, categories, and objects using a dual representation strategy.
/// Provides both human-readable string identifiers (e.g., "sparkitect:blocks:grass") and compact numeric IDs (e.g., 42:69:420).
/// </summary>
/// <remarks>
/// </remarks>
[PublicAPI]
public interface IIdentificationManager
{
    /// <summary>
    /// Registers a mod and returns its numeric ID. If already registered, returns the existing ID.
    /// </summary>
    /// <param name="modId">The string identifier for the mod (e.g., "sparkitect").</param>
    /// <returns>The numeric mod ID (ushort).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of mods (65,535) is reached.</exception>
    ushort RegisterMod(string modId);

    /// <summary>
    /// Registers a category and returns its numeric ID. If already registered, returns the existing ID.
    /// </summary>
    /// <param name="categoryId">The string identifier for the category (e.g., "blocks").</param>
    /// <returns>The numeric category ID (ushort).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of categories (65,535) is reached.</exception>
    ushort RegisterCategory(string categoryId);

    /// <summary>
    /// Resolves a string mod identifier to its numeric ID.
    /// </summary>
    /// <param name="modName">The string identifier to resolve.</param>
    /// <returns>
    ///   <see cref="Result{TOk,TError}.Ok"/> with the numeric mod ID on success;
    ///   <see cref="Result{TOk,TError}.Error"/> with <see cref="ResolveError.UnknownMod"/> if not registered.
    /// </returns>
    Result<ushort, ResolveError> GetModId(string modName);

    /// <summary>
    /// Resolves a string category identifier to its numeric ID.
    /// </summary>
    /// <param name="categoryName">The string identifier to resolve.</param>
    /// <returns>
    ///   <see cref="Result{TOk,TError}.Ok"/> with the numeric category ID on success;
    ///   <see cref="Result{TOk,TError}.Error"/> with <see cref="ResolveError.UnknownCategory"/> if not registered.
    /// </returns>
    Result<ushort, ResolveError> GetCategoryId(string categoryName);

    /// <summary>
    /// Resolves a numeric mod ID to its string identifier.
    /// </summary>
    /// <param name="modId">The numeric mod ID.</param>
    /// <returns>
    ///   <see cref="Result{TOk,TError}.Ok"/> with the string identifier on success;
    ///   <see cref="Result{TOk,TError}.Error"/> with <see cref="ResolveError.UnknownMod"/> if not registered.
    /// </returns>
    Result<string, ResolveError> GetModId(ushort modId);

    /// <summary>
    /// Resolves a numeric category ID to its string identifier.
    /// </summary>
    /// <param name="categoryId">The numeric category ID.</param>
    /// <returns>
    ///   <see cref="Result{TOk,TError}.Ok"/> with the string identifier on success;
    ///   <see cref="Result{TOk,TError}.Error"/> with <see cref="ResolveError.UnknownCategory"/> if not registered.
    /// </returns>
    Result<string, ResolveError> GetCategoryId(ushort categoryId);

    /// <summary>
    /// Registers an object within a mod:category combination and returns its full identification.
    /// If already registered, returns the existing identification.
    /// </summary>
    /// <param name="modId">The mod ID (string or numeric).</param>
    /// <param name="categoryId">The category ID (string or numeric).</param>
    /// <param name="objectId">The string identifier for the object (e.g., "stone").</param>
    /// <returns>
    /// The full <see cref="Identification"/> containing mod, category, and object numeric IDs,
    /// or <see cref="Identification.Empty"/> if mod or category is not registered.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of objects (~4.3 billion) for a mod:category combination is reached.</exception>
    Identification RegisterObject(Variant<string, ushort> modId, Variant<string, ushort> categoryId, string objectId);

    /// <summary>
    /// Resolves mod/category/object keys to an <see cref="Identification"/>.
    /// </summary>
    /// <param name="modId">The mod ID (string or numeric).</param>
    /// <param name="categoryId">The category ID (string or numeric).</param>
    /// <param name="objectId">The object ID (string or numeric).</param>
    /// <returns>
    ///   <see cref="Result{TOk,TError}.Ok"/> with the resolved <see cref="Identification"/> on success;
    ///   <see cref="Result{TOk,TError}.Error"/> with the failing component as
    ///   <see cref="ResolveError.UnknownMod"/>, <see cref="ResolveError.UnknownCategory"/>,
    ///   or <see cref="ResolveError.UnknownObject"/>. Failure check order: mod → category → object.
    /// </returns>
    Result<Identification, ResolveError> GetObjectId(
        Variant<string, ushort> modId,
        Variant<string, ushort> categoryId,
        Variant<string, ushort> objectId);

    /// <summary>
    /// Retrieves all registered object identifications across all mods and categories.
    /// </summary>
    /// <returns>An enumerable of all registered <see cref="Identification"/> instances.</returns>
    IEnumerable<Identification> GetAllObjectIds();

    /// <summary>
    /// Retrieves all object identifications registered under a specific mod, across all categories.
    /// </summary>
    /// <param name="modId">The mod ID (string or numeric) to filter by.</param>
    /// <returns>An enumerable of <see cref="Identification"/> instances for the specified mod, or empty if mod not found.</returns>
    IEnumerable<Identification> GetAllObjectIdsOfMod(Variant<string, ushort> modId);

    /// <summary>
    /// Retrieves all object identifications registered under a specific category, across all mods.
    /// </summary>
    /// <param name="categoryId">The category ID (string or numeric) to filter by.</param>
    /// <returns>An enumerable of <see cref="Identification"/> instances for the specified category, or empty if category not found.</returns>
    IEnumerable<Identification> GetAllObjectIdsOfCategory(Variant<string, ushort> categoryId);

    /// <summary>
    /// Retrieves all object identifications registered under a specific mod and category combination.
    /// </summary>
    /// <param name="modId">The mod ID (string or numeric).</param>
    /// <param name="categoryId">The category ID (string or numeric).</param>
    /// <returns>An enumerable of <see cref="Identification"/> instances for the specified combination, or empty if not found.</returns>
    IEnumerable<Identification> GetAllObjectIdsOfModAndCategory(Variant<string, ushort> modId, Variant<string, ushort> categoryId);

    /// <summary>
    /// Unregisters a mod by its numeric ID. Fails if any objects are still registered under this mod.
    /// </summary>
    /// <param name="modId">The numeric mod ID to unregister.</param>
    /// <returns><c>true</c> if the mod was successfully unregistered; <c>false</c> if not found or has dependent objects.</returns>
    bool UnregisterMod(ushort modId);

    /// <summary>
    /// Unregisters a category by its numeric ID. Fails if any objects are still registered under this category.
    /// </summary>
    /// <param name="categoryId">The numeric category ID to unregister.</param>
    /// <returns><c>true</c> if the category was successfully unregistered; <c>false</c> if not found or has dependent objects.</returns>
    bool UnregisterCategory(ushort categoryId);

    /// <summary>
    /// Unregisters a specific object by its full identification.
    /// </summary>
    /// <param name="id">The <see cref="Identification"/> of the object to unregister.</param>
    /// <returns><c>true</c> if the object was successfully unregistered; <c>false</c> if not found.</returns>
    bool UnregisterObject(Identification id);

    /// <summary>
    /// Retrieves the numeric IDs of all registered mods.
    /// </summary>
    /// <returns>An enumerable of numeric mod IDs (ushort). Use <see cref="GetModId(ushort)"/> to convert to string.</returns>
    IEnumerable<ushort> GetRegisteredMods();

    /// <summary>
    /// Retrieves the numeric IDs of all registered categories.
    /// </summary>
    /// <returns>An enumerable of numeric category IDs (ushort). Use <see cref="GetCategoryId(ushort)"/> to convert to string.</returns>
    IEnumerable<ushort> GetRegisteredCategories();

    /// <summary>
    /// Checks if a mod is registered.
    /// </summary>
    /// <param name="modId">The mod ID (string or numeric) to check.</param>
    /// <returns><c>true</c> if the mod is registered; otherwise, <c>false</c>.</returns>
    bool IsModRegistered(Variant<string, ushort> modId);

    /// <summary>
    /// Checks if a category is registered.
    /// </summary>
    /// <param name="categoryId">The category ID (string or numeric) to check.</param>
    /// <returns><c>true</c> if the category is registered; otherwise, <c>false</c>.</returns>
    bool IsCategoryRegistered(Variant<string, ushort> categoryId);

    /// <summary>
    /// Checks if an object is registered within a specific mod and category combination.
    /// </summary>
    /// <param name="modId">The mod ID (string or numeric).</param>
    /// <param name="categoryId">The category ID (string or numeric).</param>
    /// <param name="objectId">The object ID (string or numeric).</param>
    /// <returns><c>true</c> if the object is registered; otherwise, <c>false</c>.</returns>
    bool IsObjectRegistered(Variant<string, ushort> modId, Variant<string, ushort> categoryId, Variant<string, ushort> objectId);

    /// <summary>
    /// Gets the total number of registered mods.
    /// </summary>
    /// <returns>The count of registered mods.</returns>
    int GetModCount();

    /// <summary>
    /// Gets the total number of registered categories.
    /// </summary>
    /// <returns>The count of registered categories.</returns>
    int GetCategoryCount();

    /// <summary>
    /// Gets the total number of registered objects across all mods and categories.
    /// </summary>
    /// <returns>The count of all registered objects.</returns>
    int GetObjectCount();

    /// <summary>
    /// Gets the number of objects registered for a specific mod and category combination.
    /// </summary>
    /// <param name="modId">The mod ID (string or numeric).</param>
    /// <param name="categoryId">The category ID (string or numeric).</param>
    /// <returns>The count of objects in the specified mod:category combination, or 0 if not found.</returns>
    int GetObjectCountForCategory(Variant<string, ushort> modId, Variant<string, ushort> categoryId);

    /// <summary>
    /// Attempts to resolve an <see cref="Identification"/> to its string components.
    /// </summary>
    /// <param name="id">The identification to resolve.</param>
    /// <param name="modId">The string mod identifier if found, null otherwise.</param>
    /// <param name="categoryId">The string category identifier if found, null otherwise.</param>
    /// <param name="objectId">The string object identifier if found, null otherwise.</param>
    /// <returns><c>true</c> if all components were resolved; <c>false</c> if any component could not be resolved.</returns>
    bool TryResolveIdentification(Identification id, out string? modId, out string? categoryId, out string? objectId);
}