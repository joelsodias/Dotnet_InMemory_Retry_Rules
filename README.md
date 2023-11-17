# In-Memory Retry Mechanism based on Rules Sample

This little sample aims to show how to apply some concepts:

- How to create a simple Condition Builder
- How to use Condition Builder to create Business Rules for retry (SourceRulesFactory)
- How to implement in-memory queue simulator (QueueSimulator)
- How to create a retry mechanism based on Queues with attempt count control and dismissing

The project was written using .Net Core 7, but can be migrated to other versions as it uses simply code

How does it work?

- First the program creates a dataset with a start random count between 10 and 50 items to be evaluated
- After that 3 in-memory queues are created to handle waiting, retry and evaluation step
- So its is te a success percentage to random to simulate the need of retry
- BaseRuleHandlers is initialized with retry and evaluation queues
- each queueHandler is set with an action every time a new item arrives (simulation in memory with OnItemReceived event)
- to simulate concurrency a TaskList is created

The process ends when all items have been passed through mainQueue, and then by retry (if has any random failure), and after passed on evaluation stage (if success or dismissed by reach max time)

The results are written into log.txt where you can see all staps/stages for each item (successes/fails, retries/dismisses)
