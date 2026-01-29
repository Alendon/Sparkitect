using System.Threading.Tasks;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Tests;

public sealed class StringCaseTests
{
    // Valid cases
    [Test]
    [Arguments("a")]
    [Arguments("abc")]
    [Arguments("snake_case")]
    [Arguments("my_mod_id")]
    [Arguments("valid123")]
    [Arguments("id_with_123_numbers")]
    [Arguments("a1b2c3")]
    [Arguments("mod_v2")]
    public async Task IsStrictSnakeCase_ValidInput_ReturnsTrue(string input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input)).IsTrue();
    }

    // Invalid: empty/null
    [Test]
    [Arguments("")]
    [Arguments(null)]
    public async Task IsStrictSnakeCase_EmptyOrNull_ReturnsFalse(string? input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input!)).IsFalse();
    }

    // Invalid: starts with non-letter
    [Test]
    [Arguments("_leading")]
    [Arguments("123start")]
    [Arguments("_")]
    [Arguments("1abc")]
    public async Task IsStrictSnakeCase_StartsWithNonLetter_ReturnsFalse(string input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input)).IsFalse();
    }

    // Invalid: trailing underscore
    [Test]
    [Arguments("trailing_")]
    [Arguments("a_")]
    public async Task IsStrictSnakeCase_TrailingUnderscore_ReturnsFalse(string input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input)).IsFalse();
    }

    // Invalid: consecutive underscores
    [Test]
    [Arguments("double__underscore")]
    [Arguments("a__b")]
    [Arguments("some__id")]
    public async Task IsStrictSnakeCase_ConsecutiveUnderscores_ReturnsFalse(string input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input)).IsFalse();
    }

    // Invalid: uppercase letters
    [Test]
    [Arguments("PascalCase")]
    [Arguments("camelCase")]
    [Arguments("SCREAMING")]
    [Arguments("Mixed_Case")]
    public async Task IsStrictSnakeCase_UppercaseLetters_ReturnsFalse(string input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input)).IsFalse();
    }

    // Invalid: dots (namespacing not supported)
    [Test]
    [Arguments("has.dot")]
    [Arguments("namespace.id")]
    [Arguments("a.b.c")]
    public async Task IsStrictSnakeCase_ContainsDots_ReturnsFalse(string input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input)).IsFalse();
    }

    // Invalid: other special characters
    [Test]
    [Arguments("has-hyphen")]
    [Arguments("has space")]
    [Arguments("has@symbol")]
    [Arguments("has$dollar")]
    public async Task IsStrictSnakeCase_SpecialCharacters_ReturnsFalse(string input)
    {
        await Assert.That(StringCase.IsStrictSnakeCase(input)).IsFalse();
    }
}
