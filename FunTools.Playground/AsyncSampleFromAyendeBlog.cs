using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FunTools.Playground
{
    [TestFixture]
    class AsyncSampleFromAyendeBlog
    {
        [Test]
        public void UsingWebRequest()
        {
            var count = 0;
            var requestUri = new Uri("http://google.com");
            
            var sp = Stopwatch.StartNew();
            var tasks = new List<Task>();   
            for (var i = 0; i < 10; i++)
            {
                var t = Task.Run(() =>
                {
                    var webRequest = WebRequest.Create(requestUri);
                    webRequest.GetResponse().Close();
                    Interlocked.Increment(ref count);
                });
                tasks.Add(t);
            }
            var whenAll = Task.WhenAll(tasks);
            while (whenAll.IsCompleted == false && whenAll.IsFaulted  == false)
            {
                Thread.Sleep(1000);
                Console.WriteLine("{0} - {1}, {2}", sp.Elapsed, count, tasks.Count(x=> x.IsCompleted == false));
            }
            Console.WriteLine(sp.Elapsed);
        }

        [Test]
        public void UsingAsync()
        {
            var requestUri = new Uri("http://google.com");
            var tasksNumber = 10;

            var count = 0;
            var sp = Stopwatch.StartNew();
            var results = new Result<Empty>[tasksNumber];

            for (var i = 0; i < tasksNumber; i++)
            {
                var index = i;
                var task = Await.Async(() =>
                {
                    var webRequest = WebRequest.Create(requestUri);
                    webRequest.GetResponse().Close();
                    Interlocked.Increment(ref count);
                });

                task(result => result.Match(x => results[index] = x));
            }

            while (results.Any(x => x == null))
            {
                Thread.Sleep(1000);
                Console.WriteLine("{0} - {1}, {2}", sp.Elapsed, count, results.Count(x => x == null));
            }
            Console.WriteLine(sp.Elapsed);
        }
    }
}
