using FluentAssertions;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Misc.NoorAiAssistant.Tests;

/// <summary>
/// Covers the Arabic text handling behind catalogue and brand search: the <see cref="TextNormalizer"/>
/// fold that makes brand resolution spelling-insensitive, and <see cref="TextNormalizer.ArabicSearchVariants"/>
/// which un-folds a normalised term so it can still reach the raw, un-normalised product text a SQL LIKE
/// matches against.
/// </summary>
[TestFixture]
public class TextNormalizerTests
{
    #region ArabicSearchVariants

    [Test]
    public void ArabicSearchVariantsShouldExposeTheTaaMarbutaSpellingForAFinalHaa()
    {
        //"نجفه" is the folded form of the word a shopper types; the catalogue may store it as "نجفة"
        var variants = TextNormalizer.ArabicSearchVariants("نجفه");

        variants.Should().Contain("نجفة");
    }

    [Test]
    public void ArabicSearchVariantsShouldReturnTheFoldedFormFirst()
    {
        //the un-touched (folded) form must be tried first so exact matches still rank first
        var variants = TextNormalizer.ArabicSearchVariants("نجفه");

        variants[0].Should().Be("نجفه");
    }

    [Test]
    public void ArabicSearchVariantsShouldExpandAnInitialAlefToItsHamzaForms()
    {
        //a folded initial alef ("اناره") could have been written "إنارة" / "انارة" in the catalogue
        var variants = TextNormalizer.ArabicSearchVariants("اناره");

        variants[0].Should().Be("اناره");
        variants.Should().Contain("انارة");
        variants.Should().Contain("إنارة");
    }

    [Test]
    public void ArabicSearchVariantsShouldExposeTheAlefMaqsuraSpellingForAFinalYaa()
    {
        //a final "ي" may be an alef maqsura "ى" in the stored text
        var variants = TextNormalizer.ArabicSearchVariants("خارجي");

        variants.Should().Contain("خارجى");
    }

    [Test]
    public void ArabicSearchVariantsShouldLeaveANonArabicTermUnchanged()
    {
        //English words are never folded, so there is nothing to expand
        var variants = TextNormalizer.ArabicSearchVariants("chandelier");

        variants.Should().ContainSingle().Which.Should().Be("chandelier");
    }

    [Test]
    public void ArabicSearchVariantsShouldNotRepeatSpellings()
    {
        //a word with no ambiguous letter to expand ("نجف" ends in a plain ف) collapses to itself
        var variants = TextNormalizer.ArabicSearchVariants("نجف");

        variants.Should().ContainSingle().Which.Should().Be("نجف");
    }

    [Test]
    public void ArabicSearchVariantsShouldReachTheStoredSpellingOfTheReportedProduct()
    {
        //end-to-end for the reported case: a shopper types "نجفة", it folds to "نجفه", and the variant
        //set must still reach the catalogue's "نجفة"
        var typed = TextNormalizer.Normalize("نجفة");

        var variants = TextNormalizer.ArabicSearchVariants(typed);

        variants.Should().Contain("نجفة");
    }

    #endregion

    #region Brand-resolution fold guarantee

    [Test]
    public void NormalizeShouldFoldHamzaAlefFormsTogether()
    {
        //why ResolveManufacturerIdsAsync needs no variant expansion: a brand written "أوسرام" and a query
        //written "اوسرام" collapse to the same folded form, so they compare equal
        TextNormalizer.Normalize("أوسرام").Should().Be(TextNormalizer.Normalize("اوسرام"));
    }

    [Test]
    public void NormalizeShouldFoldTaaMarbutaAndHaaTogether()
    {
        TextNormalizer.Normalize("منارة").Should().Be(TextNormalizer.Normalize("مناره"));
    }

    [Test]
    public void ContainsTermShouldMatchABrandAcrossHamzaSpellings()
    {
        //the folded brand name (as ResolveManufacturerIdsAsync passes it) matched by a differently-spelled query
        var foldedBrandName = TextNormalizer.Normalize("أوسرام");

        TextNormalizer.ContainsTerm(foldedBrandName, "اوسرام").Should().BeTrue();
    }

    [Test]
    public void ContainsTermShouldMatchABrandAcrossTaaMarbutaSpellings()
    {
        var foldedBrandName = TextNormalizer.Normalize("منارة");

        TextNormalizer.ContainsTerm(foldedBrandName, "مناره").Should().BeTrue();
    }

    #endregion
}
