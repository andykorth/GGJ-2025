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

public class InvokeCommand
{
    internal static readonly Dictionary<string, MethodInfo> Commands = new();
    internal static readonly Dictionary<string, GameCommandAttribute> HelpAttrs = new();

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
            HelpAttrs[commandName] = attribute!;
        }

        Log.Info("Found commands: " + Commands.Count);
    }

    internal static void Invoke(Player p, GameUpdateService game, string command, string args)
    {
        if (Commands.TryGetValue(command.ToLower(), out var method))
        {
            try {
                game.Send(p, $"[magenta]>{command}[/magenta] {args}");
                method.Invoke(null, new object[] { p, game, args });
            }
            catch (Exception ex) {
                if(ex.InnerException != null){
                    game.Send(p, $"Error executing command [{command}]: [red]{ex.InnerException!.Message}[/red]");
                    game.Send(p, ex!.InnerException!.StackTrace!);
                    Log.Error(ex.InnerException.ToString());
                }else{
                    game.Send(p, $"Error executing command [{command}]: [red]{ex.Message}[/red]");
                    game.Send(p, ex.StackTrace!);
                    Log.Error(ex.ToString());
                }
            }
            return;
        }

        game.Send(p, $"Command [red]{command}[/red] not recognized. Type help");
    }

}
