using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


class Program
{
    static async Task Main()
    {
        var started = DateTime.Now;

        List<Source> finishedSources = new();

        const int successPercentage = 50;

        var sourceDataset = DataFactory.GetSourceDatasetFresh(10,50); 

        var mainQueueSimulator = new QueueSimulator<Source>(10, 15, "main");
        var retryQueueSimulator = new QueueSimulator<Source>(2, 4, "retry");
        var evaluationQueueSimulator = new QueueSimulator<Source>(10, 50, "evaluation");

        retryQueueSimulator.AddDependency(new List<QueueSimulator<Source>>{mainQueueSimulator});
        evaluationQueueSimulator.AddDependency(new List<QueueSimulator<Source>>{mainQueueSimulator, retryQueueSimulator});

        var printSource = delegate (Source? source, int index, string? logFileName) {
            if (source != null) {
                var first = source.FirstAttempt == null ? "Never" : (DateTime.Now - source.FirstAttempt).Value.TotalSeconds.ToString("0") + " seconds ago";
                var last = source.LastAttempt == null ? "Never" : (DateTime.Now - source.LastAttempt).Value.TotalSeconds.ToString("0") + " seconds ago";
                Console.WriteLine($"{index}:  Id {source.Id} - Cycles: {source.Cycles} - Attempts: {source.Attempts} - Key: {source?.Key} - Status: {source?.Status} - FirstAttempt: ({first}) - LastAttempt: ({last}) - Action: ({source?.Action}) - reason: ({source?.Reason})");
                foreach (var log in source.Log) {
                    Console.WriteLine($"   Log: {log}");
                }

                if (!string.IsNullOrEmpty(logFileName)) {
                    // Append the log to the specified file
                    File.AppendAllText(logFileName, $"{index}: Id {source.Id} - Cycles: {source.Cycles} - Attempts: {source.Attempts} - Key: {source?.Key} - Status: {source?.Status} - FirstAttempt: ({first}) - LastAttempt: ({last}) - Action: ({source?.Action}) - reason: ({source?.Reason})\n");
                    foreach (var log in source.Log) {
                        File.AppendAllText(logFileName, $"   Log: {log}\n");
                    }
                }
            }
        };  

		var printList = delegate (IEnumerable<Source?>? sources, string? logFileName){
            var count = 1;
                if (sources != null) {
                    foreach (var source in sources) {
                        printSource(source, count++, logFileName);
                    }
                }
                Console.WriteLine(new string('=',10));
            };

		var printStatus = delegate (){
            var elapsed = (DateTime.Now - started).TotalSeconds.ToString("0");
            Console.WriteLine($"Elapsed: {elapsed} secs - Main:  {mainQueueSimulator.QueueCount} - Retry: {retryQueueSimulator.QueueCount} - Evaluation: {evaluationQueueSimulator.QueueCount} - Finished: {finishedSources.Count}");

        };
		
		var beforeRun = delegate (ConditionBuilder<Source> condition){
                Console.WriteLine(new string('=',40));
                Console.WriteLine(condition.Name);
                Console.WriteLine(new string('=',40));
                //printList(condition.Data);
                //Console.WriteLine(new string('=',10));
            };

        var chance = delegate (int weight) {
            Random gen = new Random();
            // int prob = gen.Next(100);
            // return  prob < weight;

            return gen.NextDouble() < weight / 100.0;
        };

        BaseRuleHandlers.Initialize(retryQueueSimulator, evaluationQueueSimulator);

        var conditionList = SourceRulesFactory.GetRules();

        Action<Source, QueueSimulator<Source>> processQueueItem = delegate (Source item, QueueSimulator<Source> queue) {
            item.Cycles++;
        
            foreach (var condition in conditionList)
            {
                if (condition.RunOne(item))
                {
                    Console.WriteLine($" Stopped on '{condition.Name}' with {condition.MatchesCount} matches");
                    break;
                } else {
                    Console.WriteLine($" Stopped on '{condition.Name}' with no matches");
                }
            }

            if (mainQueueSimulator.QueueCount > 0 && (mainQueueSimulator.QueueCount + retryQueueSimulator.QueueCount + evaluationQueueSimulator.QueueCount + finishedSources.Count < 500 )) {

                var newCommers = DataFactory.GetSourceDatasetFresh(1,5); 

                foreach(var source in newCommers)
                {
                    mainQueueSimulator.Add(source);
                }
            }            
            
            printStatus();
        };

        // Subscribe to the ItemProcessed event with a custom handler
        mainQueueSimulator.OnItemReceived += 
            (item, queue) => 
                processQueueItem(item, queue);

        retryQueueSimulator.OnItemReceived += 
            (item, queue) => 
                processQueueItem(item, queue);

        evaluationQueueSimulator.OnItemReceived += 
            (item, queue) => {
                finishedSources.Add(item);
                printStatus();
            };

        // Add some items to the queue
        foreach(var source in sourceDataset)
        {
            mainQueueSimulator.Add(source);
        }

        started = DateTime.Now;
        List<Task> taskList = new();
        // Start processing the items
        taskList.Add(retryQueueSimulator.StartProcessingAsync());
        taskList.Add(mainQueueSimulator.StartProcessingAsync());
        taskList.Add(evaluationQueueSimulator.StartProcessingAsync());

        Task.WaitAll(taskList.ToArray());
        var ended = DateTime.Now;
        var elapsed = (ended - started).TotalSeconds;

        Console.WriteLine(new string('=',30));
        Console.WriteLine("FINAL");
        Console.WriteLine(new string('=',30));    
        printList(finishedSources, "log.txt");

        Console.WriteLine($"All items processed in {elapsed} seconds.");

    }
}
