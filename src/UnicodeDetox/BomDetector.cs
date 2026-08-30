using System;
using System.Text;

namespace Brx.UnicodeDetox;

public static class BomDetector
{
   public static int GetBomLen(ReadOnlySpan<byte> span)
   {
      if (span.Length >= 2)
      {
         if (span.Length >= 3)
         {
            // UTF-8 BOM: EF BB BF
            if (span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
               return 3;
         }
         if ((span[0] == 0xFF && span[1] == 0xFE) || (span[0] == 0xFE && span[1] == 0xFF))
         {
            // UTF-16 LE BOM: FF FE
            // UTF-16 BE BOM: FE FF
            return 2;
         }
      }
      return 0;
   }

   public static byte[]? GetBom(ReadOnlySpan<byte> span)
   {
      var bomLen = GetBomLen(span);
      return (bomLen > 0) ? span.Slice(0, bomLen).ToArray() : null;
   }

   public static Encoding CreateEncoding(byte[]? bom)
   {
      if (bom != null)
      {
         if (bom.Length == 3) // EF BB BF
            return new UTF8Encoding(true, true);

         if (bom.Length == 2)
         {
            if (bom[0] == 0xFF && bom[1] == 0xFE)
               return new UnicodeEncoding(false, true); // UTF-16 LE

            if (bom[0] == 0xFE && bom[1] == 0xFF)
               return new UnicodeEncoding(true, true);  // UTF-16 BE
         }
      }

      return new UTF8Encoding(false, true);
   }
}
