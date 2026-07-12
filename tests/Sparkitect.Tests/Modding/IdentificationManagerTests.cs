using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Tests.Modding;

public class IdentificationManagerTests
{
    // Registration Tests

    [Test]
    public async Task RegisterMod_NewMod_ReturnsUniqueId()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var id = manager.RegisterMod("test_mod");

        // Assert
        await Assert.That(id).IsEqualTo((ushort)1);
    }

    [Test]
    public async Task RegisterMod_SameMod_ReturnsSameId()
    {
        // Arrange
        var manager = new IdentificationManager();
        var firstId = manager.RegisterMod("test_mod");

        // Act
        var secondId = manager.RegisterMod("test_mod");

        // Assert
        await Assert.That(secondId).IsEqualTo(firstId);
    }

    [Test]
    public async Task RegisterMod_MultipleMods_ReturnsSequentialIds()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var id1 = manager.RegisterMod("mod_one");
        var id2 = manager.RegisterMod("mod_two");
        var id3 = manager.RegisterMod("mod_three");

        // Assert
        await Assert.That(id1).IsEqualTo((ushort)1);
        await Assert.That(id2).IsEqualTo((ushort)2);
        await Assert.That(id3).IsEqualTo((ushort)3);
    }

    [Test]
    public async Task RegisterCategory_NewCategory_ReturnsUniqueId()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var id = manager.RegisterCategory("blocks");

        // Assert
        await Assert.That(id).IsEqualTo((ushort)1);
    }

    [Test]
    public async Task RegisterCategory_DuplicateCategory_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new IdentificationManager();
        manager.RegisterCategory("blocks");

        // Act + Assert — second registration of the same string must throw
        await Assert.That(() => manager.RegisterCategory("blocks"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RegisterCategory_DifferentCategories_ReturnsSequentialIds()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var id1 = manager.RegisterCategory("blocks");
        var id2 = manager.RegisterCategory("items");

        // Assert
        await Assert.That(id1).IsEqualTo((ushort)1);
        await Assert.That(id2).IsEqualTo((ushort)2);
    }

    [Test]
    public async Task RegisterObject_ValidModAndCategory_ReturnsIdentification()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");

        // Act
        var identification = manager.RegisterObject(modId, catId, "stone");

        // Assert
        await Assert.That(identification).IsNotEqualTo(Identification.Empty);
        await Assert.That(identification.ModId).IsEqualTo(modId);
        await Assert.That(identification.CategoryId).IsEqualTo(catId);
        await Assert.That(identification.ItemId).IsEqualTo((uint)1);
    }

    [Test]
    public async Task RegisterObject_InvalidMod_ReturnsEmpty()
    {
        // Arrange
        var manager = new IdentificationManager();
        var catId = manager.RegisterCategory("blocks");

        // Act - use unregistered mod ID
        var identification = manager.RegisterObject((ushort)999, catId, "stone");

        // Assert
        await Assert.That(identification).IsEqualTo(Identification.Empty);
    }

    [Test]
    public async Task RegisterObject_SameObject_ReturnsSameId()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        var firstId = manager.RegisterObject(modId, catId, "stone");

        // Act
        var secondId = manager.RegisterObject(modId, catId, "stone");

        // Assert
        await Assert.That(secondId).IsEqualTo(firstId);
    }


    // Lookup Tests

    [Test]
    public async Task GetModId_RegisteredMod_ReturnsOk()
    {
        // Arrange
        var manager = new IdentificationManager();
        var expectedId = manager.RegisterMod("test_mod");

        // Act
        var result = manager.GetModId("test_mod");

        // Assert
        await Assert.That(result).IsTypeOf<Result<ushort, ResolveError>.Ok>();
        if (result is Result<ushort, ResolveError>.Ok(var id))
            await Assert.That(id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task GetModId_UnregisteredMod_ReturnsErrorUnknownMod()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.GetModId("nonexistent");

        // Assert
        await Assert.That(result).IsTypeOf<Result<ushort, ResolveError>.Error>();
        if (result is Result<ushort, ResolveError>.Error(ResolveError.UnknownMod(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)"nonexistent");
    }

    [Test]
    public async Task GetCategoryId_RegisteredCategory_ReturnsOk()
    {
        // Arrange
        var manager = new IdentificationManager();
        var expectedId = manager.RegisterCategory("blocks");

        // Act
        var result = manager.GetCategoryId("blocks");

        // Assert
        await Assert.That(result).IsTypeOf<Result<ushort, ResolveError>.Ok>();
        if (result is Result<ushort, ResolveError>.Ok(var id))
            await Assert.That(id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task GetCategoryId_UnregisteredCategory_ReturnsErrorUnknownCategory()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.GetCategoryId("nonexistent");

        // Assert
        await Assert.That(result).IsTypeOf<Result<ushort, ResolveError>.Error>();
        if (result is Result<ushort, ResolveError>.Error(ResolveError.UnknownCategory(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)"nonexistent");
    }

    [Test]
    public async Task GetObjectId_RegisteredObject_ReturnsOk()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        var expectedId = manager.RegisterObject(modId, catId, "stone");

        // Act
        var result = manager.GetObjectId(modId, catId, "stone");

        // Assert
        await Assert.That(result).IsTypeOf<Result<Identification, ResolveError>.Ok>();
        if (result is Result<Identification, ResolveError>.Ok(var id))
            await Assert.That(id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task GetObjectId_UnregisteredObject_ReturnsErrorUnknownObject()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");

        // Act - mod and category exist, but no objects ever registered under them
        var result = manager.GetObjectId(modId, catId, "stone");

        // Assert
        await Assert.That(result).IsTypeOf<Result<Identification, ResolveError>.Error>();
        if (result is Result<Identification, ResolveError>.Error(ResolveError.UnknownObject(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)"stone");
    }


    // Reverse Lookup Tests

    [Test]
    public async Task GetModId_ByNumericId_ReturnsOk()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modNumericId = manager.RegisterMod("test_mod");

        // Act
        var result = manager.GetModId(modNumericId);

        // Assert
        await Assert.That(result).IsTypeOf<Result<string, ResolveError>.Ok>();
        if (result is Result<string, ResolveError>.Ok(var modString))
            await Assert.That(modString).IsEqualTo("test_mod");
    }

    [Test]
    public async Task GetModId_ByUnregisteredNumericId_ReturnsErrorUnknownMod()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.GetModId((ushort)999);

        // Assert
        await Assert.That(result).IsTypeOf<Result<string, ResolveError>.Error>();
        if (result is Result<string, ResolveError>.Error(ResolveError.UnknownMod(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)(ushort)999);
    }

    [Test]
    public async Task GetCategoryId_ByNumericId_ReturnsOk()
    {
        // Arrange
        var manager = new IdentificationManager();
        var catNumericId = manager.RegisterCategory("blocks");

        // Act
        var result = manager.GetCategoryId(catNumericId);

        // Assert
        await Assert.That(result).IsTypeOf<Result<string, ResolveError>.Ok>();
        if (result is Result<string, ResolveError>.Ok(var catString))
            await Assert.That(catString).IsEqualTo("blocks");
    }

    [Test]
    public async Task GetCategoryId_ByUnregisteredNumericId_ReturnsErrorUnknownCategory()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.GetCategoryId((ushort)999);

        // Assert
        await Assert.That(result).IsTypeOf<Result<string, ResolveError>.Error>();
        if (result is Result<string, ResolveError>.Error(ResolveError.UnknownCategory(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)(ushort)999);
    }


    // Resolution-order tests: mod -> category -> object

    [Test]
    public async Task GetObjectId_BothModAndCategoryUnknown_FailsFastWithUnknownMod()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act - mod 999 not registered, category 999 not registered
        var result = manager.GetObjectId((ushort)999, (ushort)999, "stone");

        // Assert - mod is checked first; UnknownMod wins
        await Assert.That(result).IsTypeOf<Result<Identification, ResolveError>.Error>();
        await Assert.That(result is Result<Identification, ResolveError>.Error(ResolveError.UnknownMod _))
            .IsTrue();
        if (result is Result<Identification, ResolveError>.Error(ResolveError.UnknownMod(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)(ushort)999);
    }

    [Test]
    public async Task GetObjectId_KnownModUnknownCategoryUnknownObject_ReturnsUnknownCategory()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");

        // Act
        var result = manager.GetObjectId(modId, (ushort)999, "stone");

        // Assert
        await Assert.That(result).IsTypeOf<Result<Identification, ResolveError>.Error>();
        await Assert.That(result is Result<Identification, ResolveError>.Error(ResolveError.UnknownCategory _))
            .IsTrue();
        if (result is Result<Identification, ResolveError>.Error(ResolveError.UnknownCategory(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)(ushort)999);
    }

    [Test]
    public async Task GetObjectId_KnownModKnownCategoryUnknownObject_ReturnsUnknownObject()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");

        // Act
        var result = manager.GetObjectId(modId, catId, "nonexistent");

        // Assert
        await Assert.That(result).IsTypeOf<Result<Identification, ResolveError>.Error>();
        await Assert.That(result is Result<Identification, ResolveError>.Error(ResolveError.UnknownObject _))
            .IsTrue();
        if (result is Result<Identification, ResolveError>.Error(ResolveError.UnknownObject(var key)))
            await Assert.That(key).IsEqualTo((Variant<string, ushort>)"nonexistent");
    }


    // TryResolveIdentification Tests

    [Test]
    public async Task TryResolveIdentification_ValidId_ReturnsAllComponents()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        var objId = manager.RegisterObject(modId, catId, "stone");

        // Act
        var result = manager.TryResolveIdentification(objId, out var mod, out var cat, out var obj);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(mod).IsEqualTo("test_mod");
        await Assert.That(cat).IsEqualTo("blocks");
        await Assert.That(obj).IsEqualTo("stone");
    }

    [Test]
    public async Task TryResolveIdentification_InvalidModId_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();
        var invalidId = Identification.Create(999, 1, 1); // non-existent mod ID

        // Act
        var result = manager.TryResolveIdentification(invalidId, out var mod, out var cat, out var obj);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(mod).IsNull();
    }


    // Enumeration Tests

    [Test]
    public async Task GetAllObjectIds_MultipleRegistered_ReturnsAll()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        var id1 = manager.RegisterObject(modId, catId, "stone");
        var id2 = manager.RegisterObject(modId, catId, "dirt");
        var id3 = manager.RegisterObject(modId, catId, "grass");

        // Act
        var allIds = manager.GetAllObjectIds().ToList();

        // Assert
        await Assert.That(allIds.Count).IsEqualTo(3);
        await Assert.That(allIds).Contains(id1);
        await Assert.That(allIds).Contains(id2);
        await Assert.That(allIds).Contains(id3);
    }

    [Test]
    public async Task GetAllObjectIdsOfMod_FiltersByMod_ReturnsFiltered()
    {
        // Arrange
        var manager = new IdentificationManager();
        var mod1 = manager.RegisterMod("mod_one");
        var mod2 = manager.RegisterMod("mod_two");
        var cat = manager.RegisterCategory("blocks");
        var id1 = manager.RegisterObject(mod1, cat, "stone");
        var id2 = manager.RegisterObject(mod2, cat, "dirt");

        // Act
        var mod1Ids = manager.GetAllObjectIdsOfMod(mod1).ToList();

        // Assert
        await Assert.That(mod1Ids.Count).IsEqualTo(1);
        await Assert.That(mod1Ids).Contains(id1);
        await Assert.That(mod1Ids).DoesNotContain(id2);
    }

    [Test]
    public async Task GetAllObjectIdsOfCategory_FiltersByCategory_ReturnsFiltered()
    {
        // Arrange
        var manager = new IdentificationManager();
        var mod = manager.RegisterMod("test_mod");
        var cat1 = manager.RegisterCategory("blocks");
        var cat2 = manager.RegisterCategory("items");
        var id1 = manager.RegisterObject(mod, cat1, "stone");
        var id2 = manager.RegisterObject(mod, cat2, "sword");

        // Act
        var cat1Ids = manager.GetAllObjectIdsOfCategory(cat1).ToList();

        // Assert
        await Assert.That(cat1Ids.Count).IsEqualTo(1);
        await Assert.That(cat1Ids).Contains(id1);
        await Assert.That(cat1Ids).DoesNotContain(id2);
    }


    // Unregistration Tests

    [Test]
    public async Task UnregisterMod_NoDependents_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");

        // Act
        var result = manager.UnregisterMod(modId);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(manager.IsModRegistered(modId)).IsFalse();
    }

    [Test]
    public async Task UnregisterMod_HasDependentObjects_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        manager.RegisterObject(modId, catId, "stone"); // creates dependent object

        // Act
        var result = manager.UnregisterMod(modId);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(manager.IsModRegistered(modId)).IsTrue();
    }

    [Test]
    public async Task UnregisterMod_NotRegistered_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.UnregisterMod(999);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task UnregisterCategory_NoDependents_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var catId = manager.RegisterCategory("blocks");

        // Act
        var result = manager.UnregisterCategory(catId);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(manager.IsCategoryRegistered(catId)).IsFalse();
    }

    [Test]
    public async Task UnregisterCategory_HasDependentObjects_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        manager.RegisterObject(modId, catId, "stone"); // creates dependent object

        // Act
        var result = manager.UnregisterCategory(catId);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(manager.IsCategoryRegistered(catId)).IsTrue();
    }

    [Test]
    public async Task UnregisterCategory_NotRegistered_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.UnregisterCategory(999);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task UnregisterObject_Registered_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        var objId = manager.RegisterObject(modId, catId, "stone");

        // Act
        var result = manager.UnregisterObject(objId);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(manager.IsObjectRegistered(modId, catId, "stone")).IsFalse();
    }

    [Test]
    public async Task UnregisterObject_NotRegistered_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();
        var invalidId = Identification.Create(1, 1, 999);

        // Act
        var result = manager.UnregisterObject(invalidId);

        // Assert
        await Assert.That(result).IsFalse();
    }


    // Count Verification Tests

    [Test]
    public async Task GetModCount_AfterRegistrations_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new IdentificationManager();
        manager.RegisterMod("mod_one");
        manager.RegisterMod("mod_two");
        manager.RegisterMod("mod_three");

        // Act
        var count = manager.GetModCount();

        // Assert
        await Assert.That(count).IsEqualTo(3);
    }

    [Test]
    public async Task GetCategoryCount_AfterRegistrations_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new IdentificationManager();
        manager.RegisterCategory("blocks");
        manager.RegisterCategory("items");

        // Act
        var count = manager.GetCategoryCount();

        // Assert
        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task GetObjectCount_AfterRegistrations_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        manager.RegisterObject(modId, catId, "stone");
        manager.RegisterObject(modId, catId, "dirt");
        manager.RegisterObject(modId, catId, "grass");
        manager.RegisterObject(modId, catId, "sand");

        // Act
        var count = manager.GetObjectCount();

        // Assert
        await Assert.That(count).IsEqualTo(4);
    }


    // IsEmpty tests

    [Test]
    public async Task IsEmpty_ZeroValue_ReturnsTrue()
    {
        // Arrange
        var id = Identification.Empty;

        // Act + Assert
        await Assert.That(id.IsEmpty()).IsTrue();
    }

    [Test]
    public async Task IsEmpty_NonZeroValue_ReturnsFalse()
    {
        // Arrange
        var id = Identification.Create(1, 2, 3);

        // Act + Assert
        await Assert.That(id.IsEmpty()).IsFalse();
    }

    [Test]
    public async Task IsEmpty_PartialZeroValue_ReturnsFalse()
    {
        // Edge: only mod-id zero, others non-zero — NOT empty (matches the strict 0:0:0 contract).
        var partialZero = Identification.Create(0, 1, 1);

        // Act + Assert
        await Assert.That(partialZero.IsEmpty()).IsFalse();
    }


    // IsRegistered Tests

    [Test]
    public async Task IsModRegistered_RegisteredMod_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");

        // Act
        var result = manager.IsModRegistered(modId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsModRegistered_UnregisteredMod_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.IsModRegistered((ushort)999);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsCategoryRegistered_RegisteredCategory_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var catId = manager.RegisterCategory("blocks");

        // Act
        var result = manager.IsCategoryRegistered(catId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsObjectRegistered_RegisteredObject_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        manager.RegisterObject(modId, catId, "stone");

        // Act
        var result = manager.IsObjectRegistered(modId, catId, "stone");

        // Assert
        await Assert.That(result).IsTrue();
    }


    // Teardown-symmetry Tests: reversing every object under a mod/category must let the
    // category (and mod) unregister cleanly. Empty (mod, category) buckets left behind by
    // UnregisterObject would otherwise read as live dependents (surfaced by shader_module teardown).

    [Test]
    public async Task UnregisterCategory_AfterAllObjectsUnregistered_Succeeds()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("shader_module");
        var objId = manager.RegisterObject(modId, catId, "space_invaders");

        // Act
        var objectRemoved = manager.UnregisterObject(objId);
        var categoryRemoved = manager.UnregisterCategory(catId);

        // Assert
        await Assert.That(objectRemoved).IsTrue();
        await Assert.That(categoryRemoved).IsTrue();
    }

    [Test]
    public async Task UnregisterMod_AfterAllObjectsUnregistered_Succeeds()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("shader_module");
        var objId = manager.RegisterObject(modId, catId, "space_invaders");

        // Act
        var objectRemoved = manager.UnregisterObject(objId);
        var modRemoved = manager.UnregisterMod(modId);

        // Assert
        await Assert.That(objectRemoved).IsTrue();
        await Assert.That(modRemoved).IsTrue();
    }

    [Test]
    public async Task UnregisterCategory_WithLiveObjects_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("shader_module");
        manager.RegisterObject(modId, catId, "space_invaders");

        // Act — objects still live; the category must refuse to unregister
        var categoryRemoved = manager.UnregisterCategory(catId);

        // Assert
        await Assert.That(categoryRemoved).IsFalse();
    }

}
