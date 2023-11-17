using System.Diagnostics;
using System.Reflection;
using System;
using System.Reflection.Emit;
using System.Linq.Expressions;


public static class ReflectionHelper
{
    public static bool IsCalledFrom(Type type){
        //getting class type that called this method
        var stackTrace = new StackTrace();
        var stackFrames = stackTrace.GetFrames();

        var callingFrame = stackFrames[1];
        var method = callingFrame.GetMethod();

        //checking if the class type is GameManager
        if (method.DeclaringType.IsAssignableFrom(type))
        {
            return true;
        }
        
        return false;
    }

    public static IEnumerable<string> GetClasses(string nameSpace)
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        return asm.GetTypes()
            .Where(type => type.Namespace == nameSpace)
            .Select(type => type.Name);
    }

    public static List<Type> GetEnumsFromNamespace(string nameSpace)
    {
        List<Type> reultList = new List<Type>();
        var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());

        foreach (Type type in allTypes)
        {
            if (type.Namespace == nameSpace && type.IsEnum)
            {
                reultList.Add(type);
            }
        }

        return reultList;
    }


    public static object? GetPropertyValue(this object obj, string propertyName) {
        
        Type objType = obj.GetType();
        IList<PropertyInfo> props = new List<PropertyInfo>(objType.GetProperties());

        foreach (PropertyInfo prop in props)
        {
            if (prop.Name == propertyName) {
                return prop.GetValue(obj, null);
            }
        }

        return null;

    }

    public static Y? GetDictionaryPropertyValue<T, Y>(this Dictionary<T, Y> dict, T propertyName)
    {
        if (dict.TryGetValue(propertyName, out var _value))
        {
            return _value;
        }
        return default(Y);
    }

    public static bool IsAssemblyLoaded(string assemblyName) {
        return AppDomain.CurrentDomain.GetAssemblies()
                            .Any(assembly => !string.IsNullOrEmpty(assembly.FullName) && assembly.FullName.Contains(assemblyName));
    }

    public static string[]? SplitNamespaceClassMethod(string input)
    {
        char separator = '.';
        string[] parts = input.Split(separator);
        if (parts.Length >= 3)
        {
            string[] result = new string[3];
            result[0] = string.Join(separator, parts.Take(parts.Length - 2));
            result[1] = parts[parts.Length - 2];
            result[2] = parts[parts.Length - 1];
            return result;    
        } else if (parts.Length == 2) {
            string[] result = new string[2];
            result[0] = parts[parts.Length - 2];
            result[1] = parts[parts.Length - 1];
            return result;    
        } else if (parts.Length == 1){
            return new string[1]{ parts[0] };
        }
        return null;
    }

    public static Type FindTypeByClassName(string fullyQualifiedClassName)
    {
        try
        {
            // Get all loaded assemblies in the current application domain
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                // Find the type with the specified class name in the assembly
                Type targetType = assembly.GetType(fullyQualifiedClassName);
                
                if (targetType != null)
                {
                    return targetType; // Type found in this assembly
                }
            }

            return null; // Type not found in any loaded assembly
        }
        catch (Exception ex)
        {
            // Handle any exceptions that might occur during the process
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    public static T MethodFromString<T>(string delegateFullName) where T : Delegate
    {
        if (delegateFullName.Contains("."))
        {
            string[]? parts = ReflectionHelper.SplitNamespaceClassMethod(delegateFullName);
            if (parts.Length >= 2)
            {
                string className = string.Join(".", parts.Take(parts.Length - 1));
                string methodName = parts[parts.Length -1];

                Type targetType = ReflectionHelper.FindTypeByClassName(className);
                
                MethodInfo methodInfo = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);

                ParameterInfo[] parameters = methodInfo.GetParameters();

                return (T)Delegate.CreateDelegate(typeof(T), null, methodInfo);
            }
        }
        return default;
    }

    public static MethodInfo? MethodFromString(string methodFullName) 
    {
        if (methodFullName.Contains("."))
        {
            string[]? parts = ReflectionHelper.SplitNamespaceClassMethod(methodFullName);
            if (parts.Length >= 2)
            {
                string className = string.Join(".", parts.Take(parts.Length - 1));
                string methodName = parts[parts.Length -1];

                Type targetType = ReflectionHelper.FindTypeByClassName(className);
                
                return targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                
            }
        }
        return default;
    }

    public static object? ExecuteMethod(string methodFullName, object[] parameters)
    {
        object? result = null;

        var methodInfo = MethodFromString(methodFullName);

        if (methodInfo != null) return methodInfo.Invoke(null, parameters);
        
        return result;
    }


}

    




