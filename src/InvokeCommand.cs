using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Identity.Data;

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

        foreach (var type in contextTypes)
        {
            if (Activator.CreateInstance(type) is Context contextInstance)
            {
                string contextName = type.Name;
                allContexts[contextName] = contextInstance;

                // Find all methods with GameCommand attribute in this context
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttributes(typeof(GameCommandAttribute), false).Any());

                foreach (var method in methods)
                {
                    var attribute = method.GetCustomAttribute<GameCommandAttribute>();
                    var commandName = method.Name.ToLower();
                    
                    // Store in the context's own dictionary
                    contextInstance.Commands[commandName] = method;
                    contextInstance.HelpAttrs[commandName] = attribute!;
                }
            }
        }

        Log.Info($"Loaded {allContexts.Count} contexts.");
        foreach (var ctx in allContexts.Values)
        {
            Log.Info($"Context '{ctx.GetType().Name}' has {ctx.Commands.Count} commands.");
        }
    }

}
