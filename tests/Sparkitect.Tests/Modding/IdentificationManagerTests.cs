using Sparkitect.Modding;

namespace Sparkitect.Tests.Modding;

public class IdentificationManagerTests
{
    #region Registration Tests

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
    public async Task RegisterCategory_SameCategory_ReturnsSameId()
    {
        // Arrange
        var manager = new IdentificationManager();
        var firstId = manager.RegisterCategory("blocks");

        // Act
        var secondId = manager.RegisterCategory("blocks");

        // Assert
        await Assert.That(secondId).IsEqualTo(firstId);
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

    #endregion

    #region Lookup Tests

    [Test]
    public async Task TryGetModId_RegisteredMod_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var expectedId = manager.RegisterMod("test_mod");

        // Act
        var result = manager.TryGetModId("test_mod", out var id);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task TryGetModId_UnregisteredMod_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.TryGetModId("nonexistent", out var id);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(id).IsEqualTo((ushort)0);
    }

    [Test]
    public async Task TryGetCategoryId_RegisteredCategory_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var expectedId = manager.RegisterCategory("blocks");

        // Act
        var result = manager.TryGetCategoryId("blocks", out var id);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task TryGetCategoryId_UnregisteredCategory_ReturnsFalse()
    {
        // Arrange
        var manager = new IdentificationManager();

        // Act
        var result = manager.TryGetCategoryId("nonexistent", out var id);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(id).IsEqualTo((ushort)0);
    }

    [Test]
    public async Task TryGetObjectId_RegisteredObject_ReturnsTrue()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modId = manager.RegisterMod("test_mod");
        var catId = manager.RegisterCategory("blocks");
        var expectedId = manager.RegisterObject(modId, catId, "stone");

        // Act
        var result = manager.TryGetObjectId(modId, catId, "stone", out var id);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(id).IsEqualTo(expectedId);
    }

    #endregion

    #region Reverse Lookup Tests

    [Test]
    public async Task TryGetModId_ByNumericId_ReturnsString()
    {
        // Arrange
        var manager = new IdentificationManager();
        var modNumericId = manager.RegisterMod("test_mod");

        // Act
        var result = manager.TryGetModId(modNumericId, out var modString);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(modString).IsEqualTo("test_mod");
    }

    [Test]
    public async Task TryGetCategoryId_ByNumericId_ReturnsString()
    {
        // Arrange
        var manager = new IdentificationManager();
        var catNumericId = manager.RegisterCategory("blocks");

        // Act
        var result = manager.TryGetCategoryId(catNumericId, out var catString);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(catString).IsEqualTo("blocks");
    }

    #endregion

    #region TryResolveIdentification Tests

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

    #endregion

    #region Enumeration Tests

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

    #endregion

    #region Unregistration Tests

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

    #endregion

    #region Count Verification Tests

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

    #endregion

    #region IsRegistered Tests

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

    #endregion
}
