using System.Reflection;
using System.Linq.Dynamic;
using System.Linq.Dynamic.Core;


#nullable enable

public static class ConditionBuilderHelper
{

    public static ConditionBuilder<T> Name<T>(string name)
    {
        var conditionBuilder = new ConditionBuilder<T>().From(null);
        conditionBuilder.SetName(name);
        return conditionBuilder;
    }

    public static ConditionBuilder<T> From<T>(IEnumerable<T> source)
    {
        return new ConditionBuilder<T>().From(source);
    }

    public static ConditionBuilder<T> From<T>(T singleItem)
    {
        return new ConditionBuilder<T>().From(singleItem);
    }

    public static ConditionBuilder<T> Filter<T>(Func<T, bool> predicate)
    {
        return new ConditionBuilder<T>().From(null);
    }

    public static ConditionBuilder<T> Evaluate<T>(IEnumerable<T> source, string condition)
    {
        return new ConditionBuilder<T>().From(source).Evaluate(condition);
    }


}

public class ConditionBuilder<T>
{
    private Func<T, bool> condition;
    private IEnumerable<T?>? source;
    private Action<T, ConditionBuilder<T>> thenCallback;
    private Action<T, ConditionBuilder<T>> elseCallback;
    private Action<T,Exception, ConditionBuilder<T>> catchCallback;
    private Action<ConditionBuilder<T>> beforeRunCallback;
    private Action<ConditionBuilder<T>> afterRunCallback;
    private string key = string.Empty;
    private int lastSucceededCount = 0;
    private int lastFailedCount = 0;
    private int lastThenCount = 0;
    private int lastElseCount = 0;
    private int matchesCount = 0;
    private Exception? lastException = null;
    public string Name {get => key; set => SetName(value);}
    public IEnumerable<T?>? Data {get => source;}
    public int SucceededCount {get => lastSucceededCount;}
    public int FailedCount {get => lastFailedCount;}
    public int ThenCount {get => lastThenCount;}
    public int ElseCount {get => lastElseCount;}
    public int MatchesCount {get => matchesCount;}

    public void SetName(string name)
    {
        key = name;
    }
    public ConditionBuilder<T> From(IEnumerable<T?>? source)
    {
        this.source = source;
        this.condition = item => true; 
        return this;
    }

    public ConditionBuilder<T> From(T? singleItem)
    {
        return From(new[] { singleItem });
    }

    public ConditionBuilder<T> Filter(Func<T, bool> predicate)
    {
        var originalCondition = this.condition;
        this.condition = item => originalCondition(item) && predicate(item);
        return this;
    }

    public ConditionBuilder<T> And(Func<T, bool> otherCondition)
    {
        var originalCondition = this.condition;
        this.condition = item => originalCondition(item) && otherCondition(item);
        return this;
    }

    public ConditionBuilder<T> Or(Func<T, bool> otherCondition)
    {
        var originalCondition = this.condition;
        this.condition = item => originalCondition(item) || otherCondition(item);
        return this;
    }

    public ConditionBuilder<T> InArray(Func<T, IEnumerable<T>> arraySelector, T value)
    {
        var originalCondition = this.condition;
        this.condition = item => originalCondition(item) && (arraySelector(item)?.Contains(value) == true);
        return this;
    }

    public ConditionBuilder<T> Not()
    {
        var originalCondition = this.condition;
        this.condition = item => !originalCondition(item);
        return this;
    }

    public ConditionBuilder<T> BeforeRun(Action<ConditionBuilder<T>> callback)
    {
        this.beforeRunCallback = callback;
        return this;
    }
    public ConditionBuilder<T> AfterRun(Action<ConditionBuilder<T>> callback)
    {
        this.afterRunCallback = callback;
        return this;
    }

    private void SetCallback(ref Action<T, ConditionBuilder<T>> callback, string delegateFullName)
    {
        var newCallback = ReflectionHelper.MethodFromString<Action<T, ConditionBuilder<T>>>(delegateFullName);
        callback = newCallback;
    }

    private bool IsDelegateFullName(string delegateFullName)
    {
        return delegateFullName.Contains(".");
    }

    private void ExecuteDelegateByName(string delegateFullName, T item, ConditionBuilder<T> builder)
    {
        if (IsDelegateFullName(delegateFullName))
        {
            string[]? parts = ReflectionHelper.SplitNamespaceClassMethod(delegateFullName);
            if (parts.Length >= 2)
            {
                string className = string.Join(".", parts.Take(parts.Length - 1));
                string methodName = parts[parts.Length -1];

                Type targetType = ReflectionHelper.FindTypeByClassName(className);

                if (targetType != null)
                {
                    MethodInfo targetMethod = targetType.GetMethod(methodName);
                    if (targetMethod != null)
                    {
                        targetMethod.Invoke(null, new object[] { item, builder });
                    }
                    else
                    {
                        Console.WriteLine($"Method '{methodName}' not found in class '{className}'.");
                    }
                }
                else
                {
                    Console.WriteLine($"Class '{className}' not found.");
                }
            }
            else
            {
                Console.WriteLine($"Invalid delegate full name: '{delegateFullName}'.");
            }
        }
        else
        {
            Console.WriteLine($"Invalid delegate full name: '{delegateFullName}'.");
        }
    }
    

    public ConditionBuilder<T> Then(Action<T, ConditionBuilder<T>> callback)
    {
        this.thenCallback = callback;
        return this;
    }

    public ConditionBuilder<T> Then(string delegateName)
    {
        SetCallback(ref thenCallback, delegateName);
        return this;
    }

    public ConditionBuilder<T> Else(Action<T, ConditionBuilder<T>> callback)
    {
        this.elseCallback = callback;
        return this;
    }

    public ConditionBuilder<T> Else(string delegateName)
    {
        SetCallback(ref elseCallback, delegateName);
        return this;
    }
    public ConditionBuilder<T> Catch(Action<T,Exception, ConditionBuilder<T>> callback)
    {
        this.catchCallback = callback;
        return this;
    }

    public ConditionBuilder<T> Evaluate(string customCondition)
    {
        var lambdaExpression = System.Linq.Dynamic.Core.DynamicExpressionParser
            .ParseLambda<T, bool>(parsingConfig: null, createParameterCtor: false, expression: customCondition, values: source);

        this.condition = lambdaExpression.Compile();
        return this;
    }

    public void ClearCounters() {
        lastSucceededCount = 0;
        lastFailedCount = 0;
        lastThenCount = 0;
        lastElseCount = 0;
        matchesCount = 0;
    }

    public bool RunOne(T? item)
    {
        if (item == null) return false;

        var result = false;

        if (condition(item))
        {
            matchesCount++;
            result = true;
            if (thenCallback != null) {
                lastThenCount++;
                try {
                    thenCallback?.Invoke(item, this);
                    result = true;
                    lastSucceededCount++;
                    
                } catch (Exception ex) {
                    lastException = ex;
                    lastFailedCount++;
                    catchCallback?.Invoke(item, ex, this);
                }
            }
        } else {
            if (elseCallback != null) {
                lastElseCount++;
                try {
                    elseCallback?.Invoke(item, this);
                    lastSucceededCount++;
                } catch (Exception ex) {
                    lastException = ex;
                    lastFailedCount++;
                    catchCallback?.Invoke(item, ex, this);
                }
            }
        }
        return result;
    }

    public bool RunOne(int index)
    {
        if (this.source != null && this.source.Count() >= index && this.source.ElementAt(index) != null)
        {
            return RunOne(this.source.ElementAt(index));
        }
        return false;
    }

    public bool Run()
    {
        return RunUntil(null);
        // ClearCounters();

        // var result = false;

        // if (this.source != null)
        // {
        //     foreach (var item in this.source)
        //     {
        //         result = RunOne(item) | result;
        //     }
        // }
        // return result;
    }

    public bool RunUntil(bool? valueToStop = null, bool stopBefore = true)
    {
        ClearCounters();

        beforeRunCallback?.Invoke(this);

        var result = false;

        if (this.source != null)
        {
            foreach (var item in this.source)
            {
                var conditionResult = condition(item);

                if (valueToStop != null && conditionResult == valueToStop) {
                    if (!stopBefore) RunOne(item);
                    return true;
                } else {
                    if (valueToStop != null) {
                        RunOne(item);
                    } else {
                        result = RunOne(item) | result;
                    }
                }
            }
        }

        afterRunCallback?.Invoke(this);

        return result;
    }

}