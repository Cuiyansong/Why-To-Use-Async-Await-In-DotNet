using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitConsole
{
    class Program
    {
        static int maxWorkerThreads;
        static int maxAsyncIoThreadNum;

        static void Main(string[] args)
        {
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
            Thread.Sleep(2000);
            Console.WriteLine("Job Id: {0}, completed...", id);
        }

        static async Task AsyncJob(dynamic stateInfo)
        {
            var id = (long)stateInfo.Id;
            Console.WriteLine("Job Id: {0}, sync starting...", id);
            await Task.Delay(2000);
            Console.WriteLine("Job Id: {0}, completed...", id);
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
            }

            watch.Stop();
            Console.WriteLine("[Finsih] current awailible working thread is {0} and used {1}ms", awailableWorkingThreadCount, watch.ElapsedMilliseconds);
        }
    }
}
