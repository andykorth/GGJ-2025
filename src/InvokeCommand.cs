using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Method)]
public class GameCommandAttribute : Attribute
{
    public string helpText;

    public GameCommandAttribute(string v)
    {
        this.helpText = v;
    }
}

public class InvokeCommand
{
    internal static readonly Dictionary<string, MethodInfo> Commands = new();
    internal static readonly Dictionary<string, string> HelpTexts = new();

    // Static constructor to populate the Commands dictionary
    static InvokeCommand()
    {
        var methods = typeof(Commands).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttributes(typeof(GameCommandAttribute), false).Any());

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<GameCommandAttribute>();
            var commandName = method.Name.ToLower();
            Commands[commandName] = method;
            HelpTexts[commandName] = attribute?.helpText ?? "No description available.";
        }

        Log.Info("Found commands: " + Commands.Count);
    }

    internal static void Invoke(Player p, GameHub game, string command, string args)
    {
        if (Commands.TryGetValue(command.ToLower(), out var method))
        {
            try {
                method.Invoke(null, new object[] { p, game, args });
            }
            catch (Exception ex) {
                game.Send($"Error executing command [{command}]: {ex.Message}");
            }
            return;
        }

        game.Send($"Command [red]{command}[/red] not recognized. Type help");
    }

}
