using System;
using System.IO;
using System.Text;

namespace Brx.UnicodeDetox;

public static class UnicodeDetox
{
   public static string Detox(char ch)
   {
      return UnicodeDetoxConverter.Convert(ch);
   }

   public static string Detox(char? prevCh, char ch, char? nextCh)
   {
      return UnicodeDetoxConverter.Convert(prevCh, ch, nextCh);
   }

   public static string? Detox(string? value)
   {
      if (value == null)
         return null;

      var detox = new UnicodeDetoxConverter();
      var output = new StringWriter();
      detox.Convert(value, output);
      detox.Flush(output);
      return output.ToString();
   }

   public static void Detox(TextReader input, TextWriter output, int bufferSize)
   {
      var detox = new UnicodeDetoxConverter();
      var chars = new char[bufferSize];
      var span = chars.AsSpan();
      
      int charsRead;
      while ((charsRead = input.ReadBlock(chars)) > 0)
      {
         detox.Convert(span, 0, charsRead, output);
      }

      detox.Flush(output);
   }

   public static void Detox(Stream input, Stream output, Encoding encoding, int bufferSize)
   {
      var decoder = encoding.GetDecoder();
      var bytes = new byte[bufferSize];
      var chars = new char[bufferSize];
      var detox = new UnicodeDetoxConverter();

      using var writer = new StreamWriter(output, encoding, bufferSize);

      while (true)
      {
         int bytesRead = input.Read(bytes, 0, bytes.Length);
         bool eof = (bytesRead == 0);

         int inOffset = 0;
         int inRemaining = bytesRead;
         bool decodingComplete = false;

         while (inRemaining > 0 || (eof && !decodingComplete))
         {
            int bytesConsumed;
            int charsProduced;

            decoder.Convert(
                bytes, inOffset, inRemaining,
                chars, 0, chars.Length,
                eof,
                out bytesConsumed,
                out charsProduced,
                out decodingComplete
            );

            inOffset += bytesConsumed;
            inRemaining -= bytesConsumed;

            detox.ConvertUnchecked(chars, 0, charsProduced, writer);
         }

         if (eof) break;
      }

      detox.Flush(writer);
   }
}
