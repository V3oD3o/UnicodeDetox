using System;

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

      UnicodeDetox.Detox(ps, stdout, encoding, BUFFER_SIZE);
   }
}
