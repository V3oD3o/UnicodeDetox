using System;
using System.IO;

namespace Brx.UnicodeDetox;

internal sealed class PrefetchStream : Stream
{
   private readonly Stream inner;
   private readonly byte[] prefetch;
   private int offset;

   public PrefetchStream(Stream inner, int prefetchBytes)
   {
      this.inner = inner;

      prefetch = new byte[prefetchBytes];
      int read = inner.Read(prefetch, 0, prefetchBytes);

      if (read < prefetchBytes)
         Array.Resize(ref prefetch, read);

      offset = 0;
   }

   public ReadOnlySpan<byte> GetPrefetchSpan()
   {
      return prefetch.AsSpan(offset);
   }

   public void Skip(int count)
   {
      if (count < 0)
         throw new ArgumentOutOfRangeException(nameof(count));

      if (offset + count > prefetch.Length)
         throw new InvalidOperationException("Skip exceeds prefetched buffer.");

      offset += count;
   }

   public override int Read(byte[] buffer, int bufferOffset, int count)
   {
      int total = 0;

      if (offset < prefetch.Length)
      {
         int available = prefetch.Length - offset;
         int toCopy = Math.Min(available, count);

         Buffer.BlockCopy(prefetch, offset, buffer, bufferOffset, toCopy);

         offset += toCopy;
         bufferOffset += toCopy;
         count -= toCopy;
         total += toCopy;

         if (count == 0)
            return total;
      }

      int read = inner.Read(buffer, bufferOffset, count);
      total += read;

      return total;
   }

   public override bool CanRead => true;
   public override bool CanSeek => false;
   public override bool CanWrite => false;
   public override long Length => throw new NotSupportedException();
   public override long Position 
   { 
      get => throw new NotSupportedException(); 
      set => throw new NotSupportedException(); 
   }
   public override void Flush() => inner.Flush();
   public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
   public override void SetLength(long value) => throw new NotSupportedException();
   public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
