using Semver;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Modding;

public class ModFileIdentifierTests
{
    #region Constructor Tests

    [Test]
    public async Task Constructor_ValidArguments_SetsProperties()
    {
        // Arrange
        var id = "test_mod";
        var version = SemVersion.Parse("1.2.3", SemVersionStyles.Any);

        // Act
        var identifier = new ModFileIdentifier(id, version);

        // Assert
        await Assert.That(identifier.Id).IsEqualTo(id);
        await Assert.That(identifier.Version).IsEqualTo(version);
    }

    [Test]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        // Arrange
        var version = SemVersion.Parse("1.0.0", SemVersionStyles.Any);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModFileIdentifier(null!, version));
    }

    [Test]
    public void Constructor_NullVersion_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModFileIdentifier("test_mod", null!));
    }

    #endregion

    #region Parse Tests

    [Test]
    public async Task Parse_ValidFormat_ReturnsCorrectIdentifier()
    {
        // Act
        var identifier = ModFileIdentifier.Parse("test_mod@1.2.3");

        // Assert
        await Assert.That(identifier.Id).IsEqualTo("test_mod");
        await Assert.That(identifier.Version.Major).IsEqualTo(1);
        await Assert.That(identifier.Version.Minor).IsEqualTo(2);
        await Assert.That(identifier.Version.Patch).IsEqualTo(3);
    }

    [Test]
    public async Task Parse_PrereleaseVersion_ReturnsCorrectIdentifier()
    {
        // Act
        var identifier = ModFileIdentifier.Parse("my_mod@2.0.0-alpha.1");

        // Assert
        await Assert.That(identifier.Id).IsEqualTo("my_mod");
        await Assert.That(identifier.Version.ToString()).IsEqualTo("2.0.0-alpha.1");
    }

    [Test]
    public async Task Parse_IdWithUnderscore_ReturnsCorrectIdentifier()
    {
        // Act
        var identifier = ModFileIdentifier.Parse("my_awesome_mod@1.0.0");

        // Assert
        await Assert.That(identifier.Id).IsEqualTo("my_awesome_mod");
    }

    [Test]
    public void Parse_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ModFileIdentifier.Parse(null!));
    }

    [Test]
    public void Parse_NoAtSymbol_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ModFileIdentifier.Parse("test_mod1.0.0"));
    }

    [Test]
    public void Parse_AtAtStart_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ModFileIdentifier.Parse("@1.0.0"));
    }

    [Test]
    public void Parse_AtAtEnd_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ModFileIdentifier.Parse("test_mod@"));
    }

    [Test]
    public void Parse_InvalidVersion_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ModFileIdentifier.Parse("test_mod@invalid"));
    }

    #endregion

    #region TryParse Tests

    [Test]
    public async Task TryParse_ValidFormat_ReturnsTrueAndSetsResult()
    {
        // Act
        var success = ModFileIdentifier.TryParse("test_mod@1.2.3", out var result);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Id).IsEqualTo("test_mod");
        await Assert.That(result.Version.Major).IsEqualTo(1);
        await Assert.That(result.Version.Minor).IsEqualTo(2);
        await Assert.That(result.Version.Patch).IsEqualTo(3);
    }

    [Test]
    public async Task TryParse_NullValue_ReturnsFalse()
    {
        // Act
        var success = ModFileIdentifier.TryParse(null, out var result);

        // Assert
        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(default(ModFileIdentifier));
    }

    [Test]
    public async Task TryParse_EmptyString_ReturnsFalse()
    {
        // Act
        var success = ModFileIdentifier.TryParse("", out var result);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryParse_InvalidFormat_ReturnsFalse()
    {
        // Act
        var success = ModFileIdentifier.TryParse("invalid", out var result);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task TryParse_InvalidVersion_ReturnsFalse()
    {
        // Act
        var success = ModFileIdentifier.TryParse("test_mod@notaversion", out var result);

        // Assert
        await Assert.That(success).IsFalse();
    }

    #endregion

    #region Equality Tests

    [Test]
    public async Task Equals_SameIdAndVersion_ReturnsTrue()
    {
        // Arrange
        var id1 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));
        var id2 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));

        // Act & Assert
        await Assert.That(id1.Equals(id2)).IsTrue();
        await Assert.That(id1 == id2).IsTrue();
        await Assert.That(id1 != id2).IsFalse();
    }

    [Test]
    public async Task Equals_DifferentVersion_ReturnsFalse()
    {
        // Arrange
        var id1 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));
        var id2 = new ModFileIdentifier("test_mod", SemVersion.Parse("2.0.0", SemVersionStyles.Any));

        // Act & Assert
        await Assert.That(id1.Equals(id2)).IsFalse();
        await Assert.That(id1 == id2).IsFalse();
        await Assert.That(id1 != id2).IsTrue();
    }

    [Test]
    public async Task Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var id1 = new ModFileIdentifier("mod_one", SemVersion.Parse("1.0.0", SemVersionStyles.Any));
        var id2 = new ModFileIdentifier("mod_two", SemVersion.Parse("1.0.0", SemVersionStyles.Any));

        // Act & Assert
        await Assert.That(id1.Equals(id2)).IsFalse();
    }

    [Test]
    public async Task Equals_Object_WorksCorrectly()
    {
        // Arrange
        var id1 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));
        object id2 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));
        object notAnIdentifier = "test_mod@1.0.0";

        // Act & Assert
        await Assert.That(id1.Equals(id2)).IsTrue();
        await Assert.That(id1.Equals(notAnIdentifier)).IsFalse();
        await Assert.That(id1.Equals(null)).IsFalse();
    }

    [Test]
    public async Task GetHashCode_SameIdAndVersion_ReturnsSameHash()
    {
        // Arrange
        var id1 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));
        var id2 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));

        // Act & Assert
        await Assert.That(id1.GetHashCode()).IsEqualTo(id2.GetHashCode());
    }

    [Test]
    public async Task GetHashCode_DifferentValues_ReturnsDifferentHash()
    {
        // Arrange
        var id1 = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0", SemVersionStyles.Any));
        var id2 = new ModFileIdentifier("test_mod", SemVersion.Parse("2.0.0", SemVersionStyles.Any));

        // Act & Assert
        await Assert.That(id1.GetHashCode()).IsNotEqualTo(id2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Test]
    public async Task ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var identifier = new ModFileIdentifier("test_mod", SemVersion.Parse("1.2.3", SemVersionStyles.Any));

        // Act
        var result = identifier.ToString();

        // Assert
        await Assert.That(result).IsEqualTo("test_mod@1.2.3");
    }

    [Test]
    public async Task ToString_WithPrerelease_IncludesPrerelease()
    {
        // Arrange
        var identifier = new ModFileIdentifier("test_mod", SemVersion.Parse("1.0.0-beta.2", SemVersionStyles.Any));

        // Act
        var result = identifier.ToString();

        // Assert
        await Assert.That(result).IsEqualTo("test_mod@1.0.0-beta.2");
    }

    #endregion

    #region Round Trip Tests

    [Test]
    public async Task ParseAndToString_RoundTrips()
    {
        // Arrange
        var original = "my_mod@1.2.3-alpha.1";

        // Act
        var identifier = ModFileIdentifier.Parse(original);
        var roundTripped = identifier.ToString();

        // Assert
        await Assert.That(roundTripped).IsEqualTo(original);
    }

    #endregion
}
