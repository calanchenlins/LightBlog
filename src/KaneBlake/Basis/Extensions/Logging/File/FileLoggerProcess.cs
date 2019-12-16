using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace KaneBlake.Basis.Extensions.Logging.File
{
    public class FileLoggerProcess : IDisposable
    {
        private readonly BlockingCollection<string> _messageQueue = new BlockingCollection<string>();

        private readonly Thread _outputThread;

        private readonly FileWrite fileWrite;

        internal FileLoggerOptions Options { get; set; }

        public FileLoggerProcess(string fileName)
        {
            fileWrite = new FileWrite(fileName);
            _outputThread = new Thread(ProcessLogQueue) { IsBackground = true, Name = "File logger queue processing thread" };
            _outputThread.Start();
        }

        public virtual void EnqueueMessage(string message)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.TryAdd(message);
                    return;
                }
                catch (InvalidOperationException) { }
            }
            WriteMessage(message, true);
        }

        private void ProcessLogQueue()
        {
            try
            {
                foreach (var message in _messageQueue.GetConsumingEnumerable())
                {
                    WriteMessage(message, _messageQueue.Count == 0);
                }
            }
            catch
            {
                try
                {
                    _messageQueue.CompleteAdding();
                }
                catch { }
            }
        }

        private void WriteMessage(string message, bool flush)
        {
            fileWrite.WriteMessage(message, flush);
        }


        #region IDisposable Support
        private bool disposedValue = false;

        public void Dispose()
        {
            if (!disposedValue)
            {
                _messageQueue.CompleteAdding();
                try
                {
                    //主线程调用_outputThread.Join
                    //等待_outputThread线程结束
                    _outputThread.Join(1500);
                }
                catch (ThreadStateException) { }

                fileWrite.Dispose();

                disposedValue = true;
            }
        }
        #endregion

        private class FileWrite : IDisposable
        {
            Stream LogFileStream;

            TextWriter LogFileWriter;

            private readonly string fileName = "log.txt";

            public FileWrite(string _fileName)
            {
                fileName = _fileName.Trim()==""?"log.txt":_fileName;
                // Directory.GetCurrentDirectory() 会得到IIS目录
                // https://github.com/aspnet/AspNetCore/issues/4206
                var baseDirectory = Directory.GetParent(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName).FullName; 
                // AppDomain.CurrentDomain.BaseDirectory 输出 
                // C:\WorkStation\Code\GitHubCode\LightBlog\src\LightBlog\bin\Debug\netcoreapp3.1\
                var path = Path.Combine(baseDirectory, fileName);
                LogFileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                LogFileStream.Seek(0, SeekOrigin.End);
                LogFileWriter = new StreamWriter(LogFileStream);
            }

            internal void WriteMessage(string message, bool flush)
            {
                if (LogFileWriter != null)
                {
                    LogFileWriter.WriteLine(message);
                    if (flush)
                    {
                        LogFileWriter.Flush();
                    }
                }
            }

            #region IDisposable Support
            private bool disposedValue = false;

            public void Dispose()
            {
                if (!disposedValue)
                {
                    LogFileWriter.Dispose();
                    LogFileWriter = null;
                    LogFileStream.Dispose();
                    LogFileStream = null;
                    disposedValue = true;
                }
            }
            #endregion
        }
    }
}
