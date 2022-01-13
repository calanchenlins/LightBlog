using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.Extensions.Pipelines
{
    /// <summary>
    /// Extensions of PipeReader
    /// </summary>
    public static class PipeReaderExtensions
    {
        /// <summary>
        /// Asynchronously reads to the end of the current System.IO.Pipelines.PipeReader.
        /// Requires a call to BodyReader.Advance after use ReadResult.Buffer
        /// </summary>
        /// <param name="pipeReader"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async ValueTask<Memory<byte>> ReadToEndAsync(this PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                // Calling PipeReader.ReadAsync() Does not mean it reads the entire stream: 
                //// 1、It Start reading the stream from PipeReader.InnerStream.Position
                //// 2. Length of First buffer to read = PipeReader._readHead'length = PipeReader.InnerStream.Length - PipeReader.InnerStream.Position.
                //////  If PipeReader._readHead'length is zero, it will read the stream until it ends
                var readResult = await pipeReader.ReadAsync(cancellationToken);

                var buffer = readResult.Buffer;

                if (readResult.IsCompleted)
                {
                    var newbuffer = new byte[buffer.Length];
                    buffer.CopyTo(newbuffer);
                    pipeReader.AdvanceTo(buffer.Start);
                    return newbuffer.AsMemory();
                }
                pipeReader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
    }
}
