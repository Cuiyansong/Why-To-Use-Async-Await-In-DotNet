using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitConsole
{
    class Program
    {
        static int maxWorkerThreads;
        static int maxAsyncIoThreadNum;
        const string UserDirectory = @"files\";
        const int BufferSize = 1024 * 4;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Directory.Delete("files", true);
            };

            maxWorkerThreads = Environment.ProcessorCount;
            maxAsyncIoThreadNum = Environment.ProcessorCount;
            ThreadPool.SetMaxThreads(maxWorkerThreads, maxAsyncIoThreadNum);

            LogRunningTime(() =>
            {
                for (int i = 0; i < Environment.ProcessorCount * 2; i++)
                {
                   Task.Factory.StartNew(SyncJob, new {Id = i});
                }
            });

            Console.WriteLine("===========================================");

            LogRunningTime(() =>
            {
                for (int i = 0; i < Environment.ProcessorCount * 2; i++)
                {
                    Task.Factory.StartNew(AsyncJob, new { Id = i });
                }
            });

            Console.ReadKey();
        }

        static void SyncJob(dynamic stateInfo)
        {
            var id = (long)stateInfo.Id;
            Console.WriteLine("Job Id: {0}, sync starting...", id);

            using (FileStream sourceReader = new FileStream(UserDirectory + "BigFile.txt", FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize))
            {
                using (FileStream destinationWriter = new FileStream(UserDirectory + $"CopiedFile-{id}.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, BufferSize))
                {
                    CopyFileSync(sourceReader, destinationWriter);
                }
            }
            Console.WriteLine("Job Id: {0}, completed...", id);
        }

        static async Task AsyncJob(dynamic stateInfo)
        {
            var id = (long)stateInfo.Id;
            Console.WriteLine("Job Id: {0}, async starting...", id);

            using (FileStream sourceReader = new FileStream(UserDirectory + "BigFile.txt", FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous))
            {
                using (FileStream destinationWriter = new FileStream(UserDirectory + $"CopiedFile-{id}.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, BufferSize, FileOptions.Asynchronous))
                {
                    await CopyFilesAsync(sourceReader, destinationWriter);
                }
            }
            Console.WriteLine("Job Id: {0}, async completed...", id);
        }

        static async Task CopyFilesAsync(FileStream source, FileStream destination)
        {
            var buffer = new byte[BufferSize + 1];
            int numRead;
            while ((numRead = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await destination.WriteAsync(buffer, 0, numRead);
            }
        }

        static void CopyFileSync(FileStream source, FileStream destination)
        {
            var buffer = new byte[BufferSize + 1];
            int numRead;
            while ((numRead = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, numRead);
            }
        }

        static void LogRunningTime(Action callback)
        {
            var awailableWorkingThreadCount = 0;
            var awailableAsyncIoThreadCount = 0;

            var watch = Stopwatch.StartNew();
            watch.Start();

            callback();

            while (awailableWorkingThreadCount != maxWorkerThreads)
            {
                Thread.Sleep(500);
                ThreadPool.GetAvailableThreads(out awailableWorkingThreadCount, out awailableAsyncIoThreadCount);

                Console.WriteLine("[Alive] working thread: {0}, async IO thread: {1}", awailableWorkingThreadCount, awailableAsyncIoThreadCount);
            }

            watch.Stop();
            Console.WriteLine("[Finsih] current awailible working thread is {0} and used {1}ms", awailableWorkingThreadCount, watch.ElapsedMilliseconds);
        }
    }
}
