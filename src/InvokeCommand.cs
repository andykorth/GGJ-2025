
using System.Reflection;

[AttributeUsage(AttributeTargets.Method)]
public class GameCommandAttribute : Attribute
{
    public string helpText;
    public bool normallyHidden;

    public GameCommandAttribute(string v, bool normallyHidden = false)
    {
        this.helpText = v;
        this.normallyHidden = normallyHidden;
    }
}

internal static class InvokeCommand
{
    // Stores all context instances by their class name
    internal static Dictionary<string, Context> allContexts = new();

    // Static constructor to populate the context instances and commands
    static InvokeCommand()
    {
        // Find all classes that extend Context
        var contextTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Context)) && !t.IsAbstract);

        foreach (var type in contextTypes) {

            if (Activator.CreateInstance(type) is Context contextInstance) {
                string contextName = type.Name;
                allContexts[contextName] = contextInstance;

                // Find all methods with GameCommand attribute in this context
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy )
                    .Where(m => m.GetCustomAttributes(typeof(GameCommandAttribute), true).Length != 0);

                foreach (var method in methods)
                {
                    // Log.Info($"Context: {type} method: {method.Name} in {method.DeclaringType}");
                    var attribute = method.GetCustomAttribute<GameCommandAttribute>();
                    var commandName = method.Name.ToLower();

                    if(commandName == "exit" && contextInstance.rootContext){
                        continue;
                    }
                    
                    // Store in the context's own dictionary
                    if(!contextInstance.Commands.ContainsKey(commandName)){
                        contextInstance.Commands[commandName] = method;
                        contextInstance.HelpAttrs[commandName] = attribute!;
                    }
                }
            }
        }

        Log.Info($"Loaded {allContexts.Count} contexts.");
        foreach (var ctx in allContexts.Values)
        {
            Log.Info($"Context '{ctx.GetType().Name}' has {ctx.Commands.Count} commands.");
        }
    }

    public static void Load(){
        // calls the static initializer.
    }

    public static Context GetContext<T>()
    {
        return allContexts[typeof(T).FullName];
    }
}
