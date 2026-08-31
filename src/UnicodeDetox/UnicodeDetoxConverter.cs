using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Brx.UnicodeDetox;

public sealed partial class UnicodeDetoxConverter
{
   public const char EM_DASH = '\u2014';

   // Mapping table for typographic Unicode -> ASCII
   private static readonly Dictionary<char, string> AsciiMap = new Dictionary<char, string>()
   {
      ['\u201C'] = "\"",   // LEFT DOUBLE QUOTATION MARK
      ['\u201D'] = "\"",   // RIGHT DOUBLE QUOTATION MARK
      ['\u2018'] = "'",    // LEFT SINGLE QUOTATION MARK
      ['\u2019'] = "'",    // RIGHT SINGLE QUOTATION MARK

      ['\u2013'] = "-",    // EN DASH

      ['\u2026'] = "...",  // HORIZONTAL ELLIPSIS

      ['\u2022'] = "*",    // BULLET
      ['\u00B7'] = "*",    // MIDDLE DOT

      ['\u2192'] = "->",   // RIGHTWARDS ARROW
      ['\u2190'] = "<-",   // LEFTWARDS ARROW
      ['\u2194'] = "<->",  // LEFT RIGHT ARROW

      ['\u2713'] = "[OK]", // CHECK MARK
      ['\u2717'] = "[X]",  // BALLOT X

      ['\u2605'] = "*",    // BLACK STAR
      ['\u2606'] = "*",    // WHITE STAR

      ['\u00D7'] = "x",    // MULTIPLICATION SIGN
      ['\u00F7'] = "/",    // DIVISION SIGN
      ['\u00AB'] = "<<",   // LEFT-POINTING DOUBLE ANGLE QUOTATION MARK
      ['\u00BB'] = ">>",   // RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK
   };

   private static readonly HashSet<char> NonZeroWidthSpaces = new HashSet<char>()
   {
       '\u0020',           // SPACE
       '\u0009',           // CHARACTER TABULATION

       // --- Unicode Space_Separator (Zs) ---
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

       // OPTIONAL:
       // '\u3000',     // IDEOGRAPHIC SPACE (CJK full-width space)
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

   public void ResetLine()
   {
      _lineBuffer.Clear();
      _hasCR = false;
      _hasLF = false;
      _hasLeadingNonSyntax = false;
      _hasBacktick = false;
      _hasTilde = false;
   }

   public void Convert(char[] chars, int index, int count, TextWriter writer)
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

   public void FlushLine(TextWriter writer)
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

   private void ProcessLine(TextWriter writer)
   {
      if (_lineChunks == null)
      {
         _lineChunks = new List<ReadOnlyMemory<char>>();
      }
      foreach(var chunk in _lineBuffer.GetChunks())
      {
         _lineChunks.Add(chunk);
      }

      bool isInlineCode = false;
      int openingTickCount = 0;
      int currentTickCount = 0;
      char ch0 = '\0';
      char ch1 = '\0';
      char ch2 = '\0';

      for (int c = 0; c < _lineChunks.Count; c++)
      {
         var span = _lineChunks[c].Span;
         var length = span.Length;
         for (int i = 0; i < length; i++)
         {
            ch2 = ch1;
            ch1 = ch0;
            ch0 = span[i];

            if (ch1 == EM_DASH)
            {
               // if previous char was emdash, we have not written it yet
               if (NonZeroWidthSpaces.Contains(ch2) && NonZeroWidthSpaces.Contains(ch0))
               {
                  writer.Write('-');
               }
               else
               {
                  writer.Write(" - ");
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
               // we will write the emdash in the next round when we know what the next character is
               continue;
            }
            else if (AsciiMap.TryGetValue(ch0, out var detox))
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

      if (ch0 == EM_DASH) writer.Write(EM_DASH);
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
