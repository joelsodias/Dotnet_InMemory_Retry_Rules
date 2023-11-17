

static class BaseRuleHandlers {

	private const int successPercentage = 30;

	private static QueueSimulator<Source>? _retryQueueSimulator {get; set;}
	private static QueueSimulator<Source>? _evaluationQueueSimulator {get; set;}

	public static void Initialize(QueueSimulator<Source>? retryQueueSimulator, QueueSimulator<Source>? evaluationQueueSimulator){
		_retryQueueSimulator = retryQueueSimulator;
		_evaluationQueueSimulator = evaluationQueueSimulator;
	}
	
	private static bool GetFromProbability(int weight) {
            Random gen = new Random();
            return gen.NextDouble() < weight / 100.0;
    }	

    public static void Execute(Source item, ConditionBuilder<Source> condition)
    {
        item.Action = "RUN NOW!";
        item.LastAttempt = DateTime.Now;
        item.FirstAttempt ??= item.LastAttempt;
        item.Reason = condition.Name;
        item.Attempts++;
        var elapsed = (DateTime.Now - item.FirstAttempt).Value.TotalSeconds.ToString("0");
        item.Log.Add($"({elapsed} sec) Running - reason: {condition.Name}");

        if (GetFromProbability(successPercentage))
        {
            item.Action = "FINISHED";
            item.Status = "Success";
            item.Log.Add($"({elapsed} sec) Running result Success - Done");
            _evaluationQueueSimulator?.Add(item);
        }
        else
        {
            item.Status = "Error";
            item.Action = "RUN-ENQUEUED";
            item.Log.Add($"({elapsed} sec) Running result Error - Enqueueing...");
            _retryQueueSimulator?.Add(item);
        }
    }

    public static void Enqueue(Source item, ConditionBuilder<Source> condition)
    {
        item.Action = "ENQUEUED";
        item.Reason = condition.Name;
        var elapsed = (DateTime.Now - item.FirstAttempt).Value.TotalSeconds.ToString("0");
        item.Log.Add($"({elapsed} sec) Enqueueing... " + item.Reason);
        _retryQueueSimulator?.Add(item);
    }

    public static void Dismiss(Source item, ConditionBuilder<Source> condition)
    {
        item.Action = "DISMISSED";
        item.Reason = condition.Name;
        var elapsed = (DateTime.Now - item.FirstAttempt).Value.TotalSeconds.ToString("0");
        item.Log.Add($"({elapsed} sec) Dismissing... " + item.Reason);
        _evaluationQueueSimulator?.Add(item);
    }

}