﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Novell.Directory.Ldap.StressTests
{
    public class MultiThreadTest
    {
        private static readonly TimeSpan DefaultTestingThreadReportingPeriod = TimeSpan.FromMinutes(1);

        private readonly int _noOfThreads;
        private readonly TimeSpan _timeToRun;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<MultiThreadTest> _logger;
        private readonly TimeSpan _monitoringThreadReportingPeriod = TimeSpan.FromSeconds(300);

        private static readonly List<ExceptionInfo> Exceptions = new List<ExceptionInfo>();

        [CLSCompliant(false)]
        public MultiThreadTest(int noOfThreads, TimeSpan timeToRun, ILoggerFactory loggerFactory)
        {
            _noOfThreads = noOfThreads;
            _timeToRun = timeToRun;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<MultiThreadTest>();
        }

        public int Run()
        {
            var threads = new Thread[_noOfThreads];
            var threadDatas = new ThreadRunner[_noOfThreads];
            for (var i = 0; i < _noOfThreads; i++)
            {
                var threadRunner = new ThreadRunner( DefaultTestingThreadReportingPeriod, _loggerFactory.CreateLogger<ThreadRunner>());
                threads[i] = new Thread(threadRunner.RunLoop);
                threadDatas[i] = threadRunner;
                threads[i].Start();
            }
            var monitoringThread = new Thread(MonitoringThread);
            var monitoringThreadData = new MonitoringThreadData(threadDatas);
            monitoringThread.Start(monitoringThreadData);

            Thread.Sleep(_timeToRun);
            _logger.LogInformation("Exiting worker threads");
            foreach (var threadData in threadDatas)
            {
                threadData.ShouldStop = true;
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            _logger.LogInformation("Exiting monitoring thread");
            monitoringThreadData.WaitHandle.Set();
            monitoringThread.Join();

            var noOfRuns = threadDatas.Sum(x => x.Count);
            _logger.LogInformation(string.Format("Number of test runs = {0} on {1} threads, no of exceptions: {2}", noOfRuns,
                _noOfThreads, Exceptions.Count));
            return Exceptions.Count;
        }

        private void MonitoringThread(object param)
        {
            var monitoringThreadData = (MonitoringThreadData) param;
            do
            {
                DumpStats(monitoringThreadData);
            } while (!monitoringThreadData.WaitHandle.WaitOne(_monitoringThreadReportingPeriod));
            DumpStats(monitoringThreadData);
        }

        private void DumpStats(MonitoringThreadData monitoringThreadData)
        {
            var logMessage = new StringBuilder();
            logMessage.Append("Monitoring thread [threadId:noOfRuns:lastUpdateSecondsAgo:possibleHanging]:");
            foreach (var threadRunner in monitoringThreadData.ThreadRunners)
            {
                int threadId;
                int count;
                DateTime lastDate;
                lock (threadRunner)
                {
                    threadId = threadRunner.ThreadId;
                    count = threadRunner.Count;
                    lastDate = threadRunner.LastPingDate;
                }
                var lastUpdateSecondsAgo = (int) (DateTime.Now - lastDate).TotalSeconds;
                var possibleHanging = (lastUpdateSecondsAgo - 2 * DefaultTestingThreadReportingPeriod.TotalSeconds) > 0;
                logMessage.AppendFormat("[{0}-{1}-{2}-{3}]", threadId, count, lastUpdateSecondsAgo, possibleHanging ? "!!!!!!" : "_");
            }
            _logger.LogInformation(logMessage.ToString());
        }

        private class ThreadRunner
        {
            public int ThreadId;

            public ThreadRunner(TimeSpan testingThreadReportingPeriod, ILogger<ThreadRunner> logger)
            {
                TestingThreadReportingPeriod = testingThreadReportingPeriod;
                _logger = logger;
                Count = 0;
                ShouldStop = false;
                LastPingDate = DateTime.Now;
            }

            public DateTime LastPingDate;
            public int Count;
            public bool ShouldStop;
            private readonly TimeSpan TestingThreadReportingPeriod;
            private readonly ILogger<ThreadRunner> _logger;

            public void RunLoop()
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                var rnd = new Random();
                var i = 0;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                while (!ShouldStop)
                {
                    try
                    {
                        var test = TestsToRun.Tests[rnd.Next() % TestsToRun.Tests.Count];
                        test();
                    }
                    catch (Exception ex)
                    {
                        lock (Exceptions)
                        {
                            Exceptions.Add(new ExceptionInfo
                            {
                                Ex = ex,
                                ThreadId = Thread.CurrentThread.ManagedThreadId
                            });
                            _logger.LogError("Error in runner thread - {0}", ex);
                        }
                    }
                    i++;
                    if (stopWatch.Elapsed > TestingThreadReportingPeriod)
                    {
                        stopWatch.Stop();
                        lock (this)
                        {
                            Count = i;
                            LastPingDate = DateTime.Now;
                        }
                        stopWatch.Restart();
                    }
                }
            }
        }

        private class MonitoringThreadData
        {
            public MonitoringThreadData(ThreadRunner[] threadRunners)
            {
                ThreadRunners = threadRunners;
                WaitHandle = new AutoResetEvent(false);
            }

            public readonly EventWaitHandle WaitHandle;

            public ThreadRunner[] ThreadRunners { get; }
        }

        public class ExceptionInfo
        {
            public Exception Ex { get; set; }
            public long ThreadId { get; set; }
        }
    }
}