using System;
using System.Collections.Concurrent;
using System.Linq.Dynamic;
using System.Threading;
using System.Threading.Tasks;

public class QueueSimulator<T>
{
    private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
    private readonly Random random = new Random();
    private readonly int minDelayTime;
    private readonly int maxDelayTime;
    private readonly string _uid;
    private bool _stop = false;

    private List<QueueSimulator<T>> _dependencyList = new();

    // Define a delegate event for custom item processing
    public event Action<T, QueueSimulator<T>> OnItemReceived;

    public int QueueCount {get => queue.Count;}

    public string id {get => _uid;}

    public QueueSimulator(int minDelayTime, int maxDelayTime, string? id = null, List<QueueSimulator<T>>? dependencyList = null)
    {
        this.minDelayTime = minDelayTime;
        this.maxDelayTime = maxDelayTime;
        this._uid = id ?? Guid.NewGuid().ToString("N");
        AddDependency(dependencyList);
    }

    public void AddDependency(QueueSimulator<T> queue) {
        if (queue != null)
            this._dependencyList.Add(queue);
    }
    public void AddDependency(List<QueueSimulator<T>>? queueList) {
        if (queueList != null)
            this._dependencyList.AddRange(queueList);
    }
    
    public List<T> GetFullQueue() {
        return queue.ToList();
    }

    // Add items to the queue
    public void Add(T item)
    {
        queue.Enqueue(item);
    }

    // Start processing items from the queue
    public async Task StartProcessingAsync()
    {
        while (!_stop && (!queue.IsEmpty || (_dependencyList != null && _dependencyList.Count() > 0 && _dependencyList.Any(q => q.QueueCount > 0))))
        //while (!_stop && !queue.IsEmpty)
        {
            await Task.Yield();

            if (queue.Count > 0 && queue.TryDequeue(out T item))
            {
                // Simulate random processing time
                int DelayTime = random.Next(minDelayTime, maxDelayTime);
                try {
                    Console.WriteLine($"simulate {DelayTime} wait");
                    await Task.Delay(DelayTime);
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }

                // Raise the event to process the item
                DoOnItemReceived(item, this);
            }
        }
    }

    public void Stop() {
        _stop = true;
    }

    // Invoke the OnItemReceived event
    protected virtual void DoOnItemReceived(T item, QueueSimulator<T> queue)
    {
        OnItemReceived?.Invoke(item, this);
    }
}
