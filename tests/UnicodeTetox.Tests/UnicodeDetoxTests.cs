using NUnit.Framework;
using Brx.UnicodeDetox;
using System.IO;

namespace Brx.UnicodeDetox.Tests;

public class UnicodeDetoxTests
{
   // ---------------------------------------------------------
   // ASCII passthrough
   // ---------------------------------------------------------
   [TestCase("hello", "hello")]
   [TestCase("ASCII only text", "ASCII only text")]
   [TestCase("12345", "12345")]
   [TestCase("symbols !@#$%^&*()", "symbols !@#$%^&*()")]
   public void Ascii_passthrough(string input, string expected)
   {
      Assert.That(UnicodeDetox.Detox(input), Is.EqualTo(expected));
   }

   // ---------------------------------------------------------
   // Unicode -> ASCII mapping (single char)
   // ---------------------------------------------------------
   [TestCase('\u2026', "...", "U+2026 HORIZONTAL ELLIPSIS")]
   [TestCase('\u201C', "\"", "U+201C LEFT DOUBLE QUOTATION MARK")]
   [TestCase('\u2019', "'", "U+2019 RIGHT SINGLE QUOTATION MARK")]
   [TestCase('\u2192', "->", "U+2192 RIGHTWARDS ARROW")]
   [TestCase('\u2190', "<-", "U+2190 LEFTWARDS ARROW")]
   [TestCase('\u2194', "<->", "U+2194 LEFT RIGHT ARROW")]
   [TestCase('\u00D7', "x", "U+00D7 MULTIPLICATION SIGN")]
   public void Unicode_mapping_single_char(char ch, string expected, string description)
   {
      Assert.That(UnicodeDetox.Detox(ch), Is.EqualTo(expected), description);
   }

   // ---------------------------------------------------------
   // EM-dash normalization (U+2014)
   // ---------------------------------------------------------
   [TestCase("\u2014", "-", "standalone: '-'")]
   [TestCase("\u2014\u2014", "--", "multi standalone: '--'")]
   [TestCase("\u2014\u2014\u2014", "---", "multi standalone: '---'")]

   [TestCase("foo\u2014bar", "foo - bar", "sticky: 'foo-bar'")]
   [TestCase("\u2014bar", "- bar", "sticky: '-bar'")]
   [TestCase("foo\u2014", "foo -", "sticky: 'foo-'")]
   [TestCase("\u2014b", "- b", "sticky: '-b'")]
   [TestCase("f\u2014", "f -", "sticky: 'f-'")]

   [TestCase("foo \u2014 bar", "foo - bar", "padded: 'foo - bar'")]
   [TestCase("foo \u2014", "foo -", "padded: 'foo -'")]
   [TestCase("\u2014 bar", "- bar", "padded: '- bar'")]

   [TestCase("foo\u2014 bar", "foo- bar", "half-sticky: 'foo- bar'")]
   [TestCase("foo \u2014bar", "foo -bar", "half-sticky: 'foo -bar'")]

   [TestCase("foo\u2014\u2014bar", "foo -- bar", "multi sticky: 'foo--bar'")]
   [TestCase("foo \u2014\u2014 bar", "foo -- bar", "multi padded: 'foo -- bar'")]
   [TestCase("foo \u2014\u2014bar", "foo --bar", "multi half-sticky: 'foo --bar'")]
   [TestCase("foo\u2014\u2014 bar", "foo-- bar", "multi half-sticky: 'foo-- bar'")]

   [TestCase("foo\u2014 \u2014bar", "foo- -bar", "mixed: 'foo- -bar'")]
   [TestCase("foo \u2014 \u2014 bar", "foo - - bar", "mixed: 'foo - - bar'")]
   [TestCase("foo\u2014 \u2014 bar", "foo- - bar", "mixed: 'foo- - bar'")]
   [TestCase("foo \u2014 \u2014bar", "foo - -bar", "mixed: 'foo - -bar'")]
   public void EmdashNormalization(string input, string expected, string description)
   {
      Assert.That(UnicodeDetox.Detox(input), Is.EqualTo(expected), description);
   }


   [TestCase(' ', '\u2014', ' ', "-")]
   [TestCase(' ', '\u2014', 'a', "-")]
   [TestCase(' ', '\u2014', null, "-")]
   [TestCase(' ', '\u2014', '\u2014', "-")]
   
   [TestCase('a', '\u2014', ' ', "-")]
   [TestCase('a', '\u2014', 'a', " - ")]
   [TestCase('a', '\u2014', null, " -")]
   [TestCase('a', '\u2014', '\u2014', " -")]
   
   [TestCase(null, '\u2014', ' ', "-")]
   [TestCase(null, '\u2014', 'a', "- ")]
   [TestCase(null, '\u2014', null, "-")]
   [TestCase(null, '\u2014', '\u2014', "-")]
   
   [TestCase('\u2014', '\u2014', ' ', "-")]
   [TestCase('\u2014', '\u2014', 'a', "- ")]
   [TestCase('\u2014', '\u2014', null, "-")]
   [TestCase('\u2014', '\u2014', '\u2014', "-")]
   public void EmdashNormalization(char? prevCh, char ch, char? nextCh, string expected)
   {
      Assert.That(UnicodeDetox.Detox(prevCh, ch, nextCh), Is.EqualTo(expected));
   }

   // ---------------------------------------------------------
   // Inline code bypass (single tick)
   // ---------------------------------------------------------
   [Test]
   public void Inline_code_single_tick()
   {
      // U+2014 EM DASH inside inline code
      string input = "`foo\u2014bar`";
      string? output = UnicodeDetox.Detox(input);

      Assert.That(output, Is.EqualTo("`foo\u2014bar`"));
   }

   // ---------------------------------------------------------
   // Inline code bypass (multi-tick)
   // ---------------------------------------------------------
   [Test]
   public void Inline_code_multi_tick()
   {
      // U+2014 EM DASH inside triple-tick inline code
      string input = "```foo\u2014bar```";
      string? output = UnicodeDetox.Detox(input);

      Assert.That(output, Is.EqualTo("```foo\u2014bar```"));
   }

   // ---------------------------------------------------------
   // Escaped backticks inside inline code
   // ---------------------------------------------------------
   [Test]
   public void Inline_code_with_escaped_backtick()
   {
      // U+2014 EM DASH
      string input = "`foo\\`bar\u2014baz`";
      string? output = UnicodeDetox.Detox(input);

      Assert.That(output, Is.EqualTo("`foo\\`bar\u2014baz`"));
   }

   // ---------------------------------------------------------
   // Fenced code bypass
   // ---------------------------------------------------------
   [Test]
   public void Fenced_code_is_not_detoxed()
   {
      // U+2014 EM DASH inside fenced code
      string input =
          "```\n" +
          "foo\u2014bar\n" +
          "```\n";

      string? output = UnicodeDetox.Detox(input);

      Assert.That(output, Is.EqualTo(input));
   }

   // ---------------------------------------------------------
   // Fenced code with info string
   // ---------------------------------------------------------
   [Test]
   public void Fenced_code_with_info_string()
   {
      // U+2014 EM DASH
      string input =
          "```csharp\n" +
          "foo\u2014bar\n" +
          "```\n";

      string? output = UnicodeDetox.Detox(input);

      Assert.That(output, Is.EqualTo(input));
   }

   // ---------------------------------------------------------
   // Streaming chunk boundary correctness
   // ---------------------------------------------------------
   [Test]
   public void Streaming_chunk_boundary()
   {
      // U+2014 EM DASH split across chunks
      string expected = "foo - bar";

      var detox = new UnicodeDetoxConverter();
      var writer = new StringWriter();

      detox.Convert("foo".ToCharArray(), writer);
      detox.Convert("\u2014".ToCharArray(), writer); // EM DASH
      detox.Convert("bar".ToCharArray(), writer);
      detox.Flush(writer);

      Assert.That(writer.ToString(), Is.EqualTo(expected));
   }

   // ---------------------------------------------------------
   // Inline code split across chunks
   // ---------------------------------------------------------
   [Test]
   public void Inline_code_split_across_chunks()
   {
      // U+2014 EM DASH inside inline code
      string expected = "`foo\u2014bar`";

      var detox = new UnicodeDetoxConverter();
      var writer = new StringWriter();

      detox.Convert("`foo".ToCharArray(), writer);
      detox.Convert("\u2014".ToCharArray(), writer); // EM DASH
      detox.Convert("bar`".ToCharArray(), writer);
      detox.Flush(writer);

      Assert.That(writer.ToString(), Is.EqualTo(expected));
   }

   // ---------------------------------------------------------
   // CRLF handling
   // ---------------------------------------------------------
   [Test]
   public void CRLF_handling()
   {
      // U+2014 EM DASH
      string input = "foo\u2014bar\r\nbaz\u2014qux\ndox\r\r";
      string expected = "foo - bar\r\nbaz - qux\ndox\r\r";

      Assert.That(UnicodeDetox.Detox(input), Is.EqualTo(expected));
   }
}
