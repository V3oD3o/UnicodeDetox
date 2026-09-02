using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Brx.UnicodeDetox;

public sealed partial class UnicodeDetoxConverter
{
   private const char EM_DASH = '\u2014';

   private static class DetoxString
   {
      public const string QUOTATION_MARK = "\"";
      public const string APOSTROPHE = "'";
      public const string ANGLE_BRACKET_L = "<";
      public const string ANGLE_BRACKET_R = "<";
      public const string DBL_ANGLE_BRACKET_L = "<<";
      public const string DBL_ANGLE_BRACKET_R = "<<";
      public const string DASH = "-";
      public const string DBL_DASH = "--";
      public const string DASH_WITH_SPACES = " - ";
      public const string ASTERISK = "*";
      public const string TRIPLE_DOT = "...";
      public const string ARROW_L = "<-";
      public const string ARROW_R = "->";
      public const string ARROW_LR = "<->";
      public const string DBL_ARROW_L = "<=";
      public const string DBL_ARROW_R = "=>";
      public const string DBL_ARROW_LR = "<=>";
      public const string PIPE = "|";
      public const string SLASH = "/";
      public const string BACKSLASH = "\\";
   }

   // Mapping table for typographic Unicode -> ASCII
   private static readonly Dictionary<char, string> DetoxMap = new Dictionary<char, string>()
   {
      // English typographic quotes
      ['\u201C'] = DetoxString.QUOTATION_MARK,        // LEFT DOUBLE QUOTATION MARK
      ['\u201D'] = DetoxString.QUOTATION_MARK,        // RIGHT DOUBLE QUOTATION MARK
      ['\u2018'] = DetoxString.APOSTROPHE,            // LEFT SINGLE QUOTATION MARK
      ['\u2019'] = DetoxString.APOSTROPHE,            // RIGHT SINGLE QUOTATION MARK

      // Low-9 quotes (German, Hungarian, Czech, etc.)
      ['\u201E'] = DetoxString.QUOTATION_MARK,        // DOUBLE LOW-9 QUOTATION MARK
      ['\u201A'] = DetoxString.APOSTROPHE,            // SINGLE LOW-9 QUOTATION MARK

      // Reversed / high quotes
      ['\u201B'] = DetoxString.APOSTROPHE,            // SINGLE HIGH-REVERSED-9 QUOTATION MARK
      ['\u201F'] = DetoxString.QUOTATION_MARK,        // DOUBLE HIGH-REVERSED-9 QUOTATION MARK

      // Heavy ornamental quotes
      ['\u275B'] = DetoxString.APOSTROPHE,            // HEAVY SINGLE TURNED COMMA QUOTATION MARK
      ['\u275C'] = DetoxString.APOSTROPHE,            // HEAVY SINGLE COMMA QUOTATION MARK
      ['\u275D'] = DetoxString.QUOTATION_MARK,        // HEAVY DOUBLE TURNED COMMA QUOTATION MARK
      ['\u275E'] = DetoxString.QUOTATION_MARK,        // HEAVY DOUBLE COMMA QUOTATION MARK

      // Fullwidth compatibility quotes
      ['\uFF02'] = DetoxString.QUOTATION_MARK,        // FULLWIDTH QUOTATION MARK
      ['\uFF07'] = DetoxString.APOSTROPHE,            // FULLWIDTH APOSTROPHE

      // Prime-style quotes
      ['\u2032'] = DetoxString.APOSTROPHE,            // PRIME
      ['\u2033'] = DetoxString.QUOTATION_MARK,        // DOUBLE PRIME
      ['\u301D'] = DetoxString.QUOTATION_MARK,        // REVERSED DOUBLE PRIME QUOTATION MARK
      ['\u301E'] = DetoxString.QUOTATION_MARK,        // DOUBLE PRIME QUOTATION MARK
      ['\u301F'] = DetoxString.QUOTATION_MARK,        // LOW DOUBLE PRIME QUOTATION MARK

      // Rare reversed low quotes
      ['\u2E42'] = DetoxString.QUOTATION_MARK,        // DOUBLE LOW-REVERSED-9 QUOTATION MARK

      // French / Swiss guillemets
      ['\u00AB'] = DetoxString.DBL_ANGLE_BRACKET_L,   // LEFT-POINTING DOUBLE ANGLE QUOTATION MARK
      ['\u00BB'] = DetoxString.DBL_ANGLE_BRACKET_R,   // RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK
      ['\u2039'] = DetoxString.ANGLE_BRACKET_L,       // SINGLE LEFT-POINTING ANGLE QUOTATION MARK
      ['\u203A'] = DetoxString.ANGLE_BRACKET_R,       // SINGLE RIGHT-POINTING ANGLE QUOTATION MARK

      // Angle ornamental quotes
      ['\u276E'] = DetoxString.ANGLE_BRACKET_L,       // HEAVY LEFT-POINTING ANGLE QUOTATION MARK
      ['\u276F'] = DetoxString.ANGLE_BRACKET_R,       // HEAVY RIGHT-POINTING ANGLE QUOTATION MARK

      // Dashes
      ['\u2013'] = DetoxString.DASH,                  // EN DASH
      ['\u2015'] = DetoxString.DBL_DASH,              // HORIZONTAL BAR
      ['\u2212'] = DetoxString.DASH,                  // MINUS SIGN

      // Ellipsis
      ['\u2026'] = DetoxString.TRIPLE_DOT,            // HORIZONTAL ELLIPSIS

      // Bullets
      ['\u2022'] = DetoxString.ASTERISK,              // BULLET
      ['\u00B7'] = DetoxString.ASTERISK,              // MIDDLE DOT
      ['\u2605'] = DetoxString.ASTERISK,              // BLACK STAR
      ['\u2606'] = DetoxString.ASTERISK,              // WHITE STAR
      ['\u2219'] = DetoxString.ASTERISK,              // BULLET OPERATOR

      // Arrows
      ['\u2192'] = DetoxString.ARROW_R,               // RIGHTWARDS ARROW
      ['\u2190'] = DetoxString.ARROW_L,               // LEFTWARDS ARROW
      ['\u2194'] = DetoxString.ARROW_LR,              // LEFT RIGHT ARROW
      ['\u21D2'] = DetoxString.DBL_ARROW_R,           // RIGHTWARDS DOUBLE ARROW
      ['\u21D0'] = DetoxString.DBL_ARROW_L,           // LEFTWARDS DOUBLE ARROW
      ['\u21D4'] = DetoxString.DBL_ARROW_LR,          // LEFT RIGHT DOUBLE ARROW

      // Vertical separators
      ['\u2758'] = DetoxString.PIPE,                  // LIGHT VERTICAL BAR
      ['\u2759'] = DetoxString.PIPE,                  // MEDIUM VERTICAL BAR
      ['\u275A'] = DetoxString.PIPE,                  // HEAVY VERTICAL BAR
      ['\uFFE8'] = DetoxString.PIPE,                  // HALFWIDTH FORMS LIGHT VERTICAL
      ['\uFF5C'] = DetoxString.PIPE,                  // FULLWIDTH VERTICAL LINE

      // Slashes
      ['\u00F7'] = DetoxString.SLASH,                 // DIVISION SIGN
      ['\u2044'] = DetoxString.SLASH,                 // FRACTION SLASH
      ['\u2215'] = DetoxString.SLASH,                 // DIVISION SLASH
      ['\u2216'] = DetoxString.BACKSLASH,             // SET MINUS
      ['\u29F5'] = DetoxString.BACKSLASH,             // REVERSE SOLIDUS OPERATOR
      ['\u29F9'] = DetoxString.BACKSLASH,             // BIG REVERSE SOLIDUS
      ['\uFF3C'] = DetoxString.BACKSLASH,             // FULLWIDTH REVERSE SOLIDUS

      ['\u2713'] = "[OK]",                            // CHECK MARK
      ['\u2717'] = "[X]",                             // BALLOT X

      ['\u00D7'] = "x",                               // MULTIPLICATION SIGN
   };

   private static readonly HashSet<char> NonZeroWidthSpaces = new HashSet<char>()
   {
      // Normal horizontal whitespace chars
       '\u0020',           // SPACE
       '\u0009',           // CHARACTER TABULATION

       // Unicode space separator (Zs)
       '\u00A0',           // NO-BREAK SPACE
       '\u1680',           // OGHAM SPACE MARK
       '\u2000',           // EN QUAD
       '\u2001',           // EM QUAD
       '\u2002',           // EN SPACE
       '\u2003',           // EM SPACE
       '\u2004',           // THREE-PER-EM SPACE
       '\u2005',           // FOUR-PER-EM SPACE
       '\u2006',           // SIX-PER-EM SPACE
       '\u2007',           // FIGURE SPACE
       '\u2008',           // PUNCTUATION SPACE
       '\u2009',           // THIN SPACE
       '\u200A',           // HAIR SPACE
       '\u202F',           // NARROW NO-BREAK SPACE
       '\u205F',           // MEDIUM MATHEMATICAL SPACE
       
       // Additional spaces
       '\u3000',           // IDEOGRAPHIC SPACE (CJK full-width space)
   };

   [GeneratedRegex(@"^(?: {0,3})(?<fence>`{3,}|~{3,})(?<info>[^`]*?)\s*$", RegexOptions.Compiled)]
   private static partial Regex GetOpeningFencePattern();
   private static readonly Regex openingFencePattern = GetOpeningFencePattern();
   private static readonly int openingFenceGroupIndex = openingFencePattern.GroupNumberFromName("fence");

   [GeneratedRegex(@"^(?: {0,3})(?<fence>`{3,}|~{3,})\s*$", RegexOptions.Compiled)]
   private static partial Regex GetClosingFencePattern();
   private static readonly Regex closingFencePattern = GetClosingFencePattern();
   private static readonly int closingFenceGroupIndex = closingFencePattern.GroupNumberFromName("fence");

   private readonly StringBuilder _lineBuffer = new StringBuilder();
   
   private List<ReadOnlyMemory<char>>? _lineChunks = null;

   private bool _hasCR;
   private bool _hasLF;

   private bool _hasLeadingNonSyntax;
   private bool _hasBacktick;
   private bool _hasTilde;

   private bool _isFenced;
   private char _fenceChar;
   private int _fenceLength;

   public static string Convert(char ch)
   {
      if (ch == EM_DASH)
      {
         return DetoxString.DASH_WITH_SPACES;
      }
      else if (ch >= 0x80 && DetoxMap.TryGetValue(ch, out var result)) 
      {
         return result;
      }
      else
      {
         return ch.ToString();
      }
   }

   public void Convert(string? value, TextWriter writer)
   {
      ArgumentNullException.ThrowIfNull(writer);
      if (value != null)
      {
         ConvertUnchecked((ReadOnlySpan<char>)value, 0, value.Length, writer);
      }
   }

   public void Convert(char[] chars, TextWriter writer)
   {
      ArgumentNullException.ThrowIfNull(chars);
      ArgumentNullException.ThrowIfNull(writer);
      ConvertUnchecked((ReadOnlySpan<char>)chars, 0, chars.Length, writer);
   }

   public void Convert(char[] chars, int index, int count, TextWriter writer)
   {
      ArgumentNullException.ThrowIfNull(chars);
      Convert((ReadOnlySpan<char>)chars, index, count, writer);
   }

   public void Convert(ReadOnlySpan<char> chars, int index, int count, TextWriter writer)
   {
      ArgumentNullException.ThrowIfNull(writer);
      ArgumentOutOfRangeException.ThrowIfNegative(index);
      ArgumentOutOfRangeException.ThrowIfNegative(count);
      ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, chars.Length);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(count, chars.Length - index);
      ConvertUnchecked(chars, index, count, writer);
   }

   public void ConvertUnchecked(ReadOnlySpan<char> chars, int index, int count, TextWriter writer)
   {
      for (int i = 0; i < count; i++)
      {
         var ch = chars[index++];
         switch (ch)
         {
            case '\r':
               if (_hasCR || _hasLF)
               {
                  FlushLine(writer);
               }
               _hasCR = true;
               continue;

            case '\n':
               if (_hasLF)
               {
                  FlushLine(writer);
               }
               _hasLF = true;
               continue;

            case '`':
               _hasBacktick = true;
               break;

            case '~':
               _hasTilde = true;
               break;

            default:
               if (_lineBuffer.Length == 0 && ch != ' ')
               {
                  _hasLeadingNonSyntax = true;
               }
               break;
         }
         if (_hasCR || _hasLF)
         {
            FlushLine(writer);
         }
         _lineBuffer.Append(ch);
      }
   }

   public void Flush(TextWriter writer)
   {
      ArgumentNullException.ThrowIfNull(writer);
      FlushLine(writer);
   }

   public void Reset()
   {
      ResetLine();
      _lineChunks = null;
      _isFenced = false;
      _fenceChar = '\0';
      _fenceLength = 0;
   }

   private void FlushLine(TextWriter writer)
   {
      if (_lineBuffer.Length > 0)
      {
         if (_isFenced)
         {
            writer.Write(_lineBuffer);

            if (IsClosingFence())
            {
               _isFenced = false;
            }
         }
         else
         {
            if (IsOpeningFence())
            {
               _isFenced = true;
               writer.Write(_lineBuffer);
            }
            else
            {
               ProcessLine(writer);
            }
         }
      }

      if (_hasCR) writer.Write('\r');
      if (_hasLF) writer.Write('\n');

      ResetLine();
   }

   private void ResetLine()
   {
      _lineBuffer.Clear();
      _hasCR = false;
      _hasLF = false;
      _hasLeadingNonSyntax = false;
      _hasBacktick = false;
      _hasTilde = false;
   }

   private void ProcessLine(TextWriter writer)
   {
      if (_lineChunks == null)
      {
         _lineChunks = new List<ReadOnlyMemory<char>>();
      }
      foreach (var chunk in _lineBuffer.GetChunks())
      {
         _lineChunks.Add(chunk);
      }

      bool isInlineCode = false;
      int openingTickCount = 0;
      int currentTickCount = 0;
      int pendingEmDashCount = 0;
      int pos1 = -2;
      char ch0 = '\0';
      char ch1 = '\0';
      char ch2 = '\0';

      for (int c = 0; c < _lineChunks.Count; c++)
      {
         var span = _lineChunks[c].Span;
         var length = span.Length;
         for (int i = 0; i < length; i++)
         {
            var ch = span[i];

            pos1++;

            if (pendingEmDashCount > 0 && ch == EM_DASH)
            {
               // extend emdash-run without rotating ch2,ch1,ch0 to preserve context
               pendingEmDashCount++;
               continue;
            }

            ch2 = ch1;
            ch1 = ch0;
            ch0 = ch;

            if (pendingEmDashCount > 0)
            {
               // check emdash-run surroundings
               bool noSpaceLeft = (pos1 == 0) || !NonZeroWidthSpaces.Contains(ch2);
               bool noSpaceRight = !NonZeroWidthSpaces.Contains(ch0);
               bool insertSpace = noSpaceLeft && noSpaceRight;

               if (insertSpace && (pos1 > 0))
               {
                  // insert padding before
                  writer.Write(' ');
               }

               // emit detoxed emdash-run
               do
               {
                  writer.Write('-');
               }
               while (--pendingEmDashCount > 0);

               if (insertSpace)
               {
                  // insert padding before
                  writer.Write(' ');
               }
            }

            if (ch0 == '`' && ch1 != '\\')
            {
               // unescaped tick -> write to the output and start counting how many we have in a row
               writer.Write(ch0);
               currentTickCount++;
               continue;
            }

            if (currentTickCount > 0)
            {
               // end of tick run
               if (!isInlineCode)
               {
                  if (FindTickRun(c, span, i + 1, currentTickCount))
                  {
                     // we have a matching tick run before the end of the line -> enter inline code
                     openingTickCount = currentTickCount;
                     isInlineCode = true;
                  }
               }
               else if (openingTickCount == currentTickCount)
               {
                  // same number of tick as the opening run -> found proper closing for the inline run
                  isInlineCode = false;
               }
               // reset tick count
               currentTickCount = 0;
            }

            if (isInlineCode || ch0 < 0x80)
            {
               // we are in an inline code section or ch0 is ASCII -> do not detox
               writer.Write(ch0);
            }
            else if (ch0 == EM_DASH)
            {
               // we will write the emdash later when we know what the next non-emdash character is
               pendingEmDashCount++;
               continue;
            }
            else if (DetoxMap.TryGetValue(ch0, out var detox))
            {
               writer.Write(detox);
            }
            else
            {
               writer.Write(ch0);
            }
         }
      }

      _lineChunks.Clear();

      if (pendingEmDashCount > 0)
      {
         if (!NonZeroWidthSpaces.Contains(ch1))
         {
            // insert padding before
            writer.Write(' ');
         }

         // emit detoxed emdash-run
         do
         {
            writer.Write('-');
         }
         while (--pendingEmDashCount > 0);
      }
   }

   private bool FindTickRun(int chunkIndex, ReadOnlySpan<char> span, int startIndex, int exactTickCount)
   {
      var tickCount = 0;
      var escape = false;
      while (true)
      {
         var length = span.Length;
         for (int i = startIndex; i < length; i++)
         {
            var ch = span[i];
            if (escape)
            {
               escape = false;
            }
            else if (ch == '\\')
            {
               escape = true;
            }
            else if (ch == '`')
            {
               tickCount++;
            }
            else if (tickCount == exactTickCount)
            {
               return true;
            }
         }
         if (++chunkIndex >= _lineChunks!.Count)
         {
            return (tickCount == exactTickCount);
         }
         span = _lineChunks[chunkIndex].Span;
         startIndex = 0;
      }
   }

   private bool IsOpeningFence()
   {
      if (!_hasLeadingNonSyntax && (_hasBacktick || _hasTilde))
      {
         var match = openingFencePattern.Match(_lineBuffer.ToString());
         if (match.Success)
         {
            var fence = match.Groups[openingFenceGroupIndex];
            _fenceLength = fence.Length;
            _fenceChar = fence.ValueSpan[0];
            return true;
         }
      }
      return false;
   }

   private bool IsClosingFence()
   {
      if (!_hasLeadingNonSyntax)
      {
         if ((_hasBacktick && !_hasTilde && _fenceChar == '`') ||
             (_hasTilde && !_hasBacktick && _fenceChar == '~'))
         {
            var match = closingFencePattern.Match(_lineBuffer.ToString());
            if (match.Success)
            {
               var fence = match.Groups[closingFenceGroupIndex];
               if (fence.Length >= _fenceLength)
               {
                  return true;
               }
            }
         }
      }
      return false;
   }
}
