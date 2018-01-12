using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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

        async static Task<int> AccessTheWebAsync()
        {
            // You need to add a reference to System.Net.Http to declare client.
            HttpClient client = new HttpClient();

            // GetStringAsync returns a Task<string>. That means that when you await the
            // task you'll get a string (urlContents).
//            Task<string> getStringTask = client.GetStringAsync("http://msdn.microsoft.com");
            Task<string> getStringTask = DoAsync();

            // You can do work here that doesn't rely on the string from GetStringAsync.

            // The await operator suspends AccessTheWebAsync.
            //  - AccessTheWebAsync can't continue until getStringTask is complete.
            //  - Meanwhile, control returns to the caller of AccessTheWebAsync.
            //  - Control resumes here when getStringTask is complete.
            //  - The await operator then retrieves the string result from getStringTask.
            string urlContents = await getStringTask;

            // The return statement specifies an integer result.
            // Any methods that are awaiting AccessTheWebAsync retrieve the length value.
            return urlContents.Length;
        }

        static void DoIndependentWork()
        {
            Console.WriteLine("Do some working...");
        }

        async static Task<string> DoAsync()
        {
            System.Threading.Thread.Sleep(10000);

            Console.WriteLine("Sending http request...");
            return "1024";
        }

        static async void wrapper()
        {
            var result = await AccessTheWebAsync();
            DoIndependentWork();
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Directory.Delete("files", true);
            };

            maxWorkerThreads = Environment.ProcessorCount;
            maxAsyncIoThreadNum = Environment.ProcessorCount;
            ThreadPool.SetMaxThreads(maxWorkerThreads, maxAsyncIoThreadNum);


            Console.WriteLine("===================== Sync Job ======================");

            LogRunningTime(() =>
            {
                for (int i = 0; i < Environment.ProcessorCount * 2; i++)
                {
                    Task.Factory.StartNew(SyncJob, new {Id = i});
                }
            });

            Console.WriteLine("===================== Aysnc Job ======================");

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

            while (maxWorkerThreads != awailableWorkingThreadCount)
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
