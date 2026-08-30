using System;
using System.IO;
using System.Text;

namespace Brx.UnicodeDetox;

public class Program
{
   private const int BUFFER_SIZE = 4096;

   public static void Main()
   {
      var stdin = Console.OpenStandardInput();
      var stdout = Console.OpenStandardOutput();

      var ps = new PrefetchStream(stdin, 3);

      byte[]? bom = BomDetector.GetBom(ps.GetPrefetchSpan());
      if (bom != null)
         ps.Skip(bom.Length);

      var encoding = BomDetector.CreateEncoding(bom);

      UnicodeDetox(ps, stdout, encoding, BUFFER_SIZE);
   }

   private static void UnicodeDetox(Stream input, Stream output, Encoding encoding, int bufferSize)
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

            detox.Convert(chars, 0, charsProduced, writer);
         }

         if (eof) break;
      }

      detox.FlushLine(writer);
   }
}
