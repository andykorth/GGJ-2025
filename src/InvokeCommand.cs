using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Identity.Data;

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
                game.Send($"[magenta]>{command}[/magenta] {args}");
                method.Invoke(null, new object[] { p, game, args });
            }
            catch (Exception ex) {
                if(ex.InnerException != null){
                    game.Send($"Error executing command [{command}]: {ex.InnerException!.Message}");
                    game.Send(ex!.InnerException!.StackTrace!);
                    Log.Error(ex.InnerException.ToString());
                }else{
                    game.Send($"Error executing command [{command}]: {ex.Message}");
                    game.Send(ex.StackTrace!);
                    Log.Error(ex.ToString());
                }
            }
            return;
        }

        game.Send($"Command [red]{command}[/red] not recognized. Type help");
    }

}
