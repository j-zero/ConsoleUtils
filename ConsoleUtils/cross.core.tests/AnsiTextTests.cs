using Pastel;
using Xunit;

public class AnsiTextTests
{
    [Fact]
    public void PastelAddsControlCodesButNotVisibleLength()
    {
        string colored = "hello".Pastel("#ff5d8f");
        Assert.True(colored.Length > 5);
        Assert.Equal(5, colored.VisibleLength());
    }

    [Fact]
    public void NestedAndBackgroundColors()
    {
        string s = ("ab" + "cd".Pastel("#00ff00") + "ef").Pastel("#ff0000").PastelBg("#0000ff");
        Assert.Equal(6, s.VisibleLength());
        Assert.Equal("abcdef", s.Strip());
        Assert.Equal("abcdef", s.Plain());
    }

    [Fact]
    public void StripHandlesAnyCsiAndOscSequence()
    {
        Assert.Equal("text", "\u001b[1;31mtext\u001b[0m".Strip());
        Assert.Equal("text", "\u001b[2Ktext\u001b[3A".Strip());
        Assert.Equal("link", "\u001b]8;;https://example.org\u0007link\u001b]8;;\u0007".Strip());
        Assert.Equal("link", "\u001b]8;;https://example.org\u001b\\link\u001b]8;;\u001b\\".Strip());
    }

    [Fact]
    public void PlainTextIsUnchanged()
    {
        Assert.Equal("plain", "plain".Strip());
        Assert.False("plain".HasAnsi());
        Assert.True("a".Pastel("#fff").HasAnsi());
        Assert.Equal(0, "".VisibleLength());
        Assert.Equal(0, ((string)null).VisibleLength());
    }

    [Fact]
    public void WideAndCombiningCharacters()
    {
        Assert.Equal(1, "ä".VisibleLength());
        Assert.Equal(1, "ä".VisibleLength());       // a + combining diaeresis
        Assert.Equal(4, "日本".VisibleLength());           // two wide characters
        Assert.Equal(2, "😀".VisibleLength());             // surrogate pair, drawn two cells wide
        Assert.Equal(2, "a​b".VisibleLength());      // zero width space
    }

    [Fact]
    public void PadRightAndLeftUseTheVisibleWidth()
    {
        string name = "ab".Pastel("#ff0000");
        string right = name.PadRightVisible(6) + "|";
        string left = name.PadLeftVisible(6) + "|";
        Assert.Equal("ab    |", right.Strip());
        Assert.Equal("    ab|", left.Strip());
        Assert.Equal(7, right.VisibleLength());
        Assert.Equal("long", "long".PadRightVisible(2));  // never shortens
    }

    [Fact]
    public void Center()
    {
        Assert.Equal(" ab  ", "ab".Pastel("#fff").CenterVisible(5).Strip());
    }

    [Fact]
    public void TruncateKeepsColorsAndResets()
    {
        string s = "abcdefghij".Pastel("#ff0000");
        string t = s.TruncateVisible(5);
        Assert.Equal(5, t.VisibleLength());
        Assert.Equal("abcd…", t.Strip());
        Assert.EndsWith("\u001b[0m", t);
        Assert.StartsWith("\u001b[38;2;255;0;0m", t);
    }

    [Fact]
    public void TruncateReturnsShortStringsUnchanged()
    {
        string s = "abc".Pastel("#00ff00");
        Assert.Same(s, s.TruncateVisible(10));
        Assert.Equal("abc", "abc".TruncateVisible(3));
    }

    [Fact]
    public void TruncateEdgeCases()
    {
        Assert.Equal("", "abc".TruncateVisible(0));
        Assert.Equal("a", "abc".TruncateVisible(1, ""));
        Assert.Equal("…", "abc".TruncateVisible(1));
        Assert.Equal("a", "abc".TruncateVisible(1, "..."));   // the ellipsis does not fit, so it is left out
        Assert.Equal("ab...", "abcdefg".TruncateVisible(5, "..."));
        Assert.Equal("日…", "日本語".TruncateVisible(3));       // a wide character is never cut in half
    }
}
