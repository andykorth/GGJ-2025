using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class Commands
{
    
    [GameCommand("Chat with all other users.")]
    public static void Say(Player p, GameHub game, string args)
    {
        game.SendAll($"{p.name}: " + args);
    }
    
    [GameCommand("Share an image with all users.")]
    public static void ShareImage(Player p, GameHub game, string args)
    {
        if (Uri.TryCreate(args, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            game.SendImage(uriResult.ToString());
        } else {
            game.Send($"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
    }

    [GameCommand("See this help message.")]
    public static void Help(Player p, GameHub game, string args)
    {
        string msg = "These are the commands you can type:\n";
        foreach(var method in InvokeCommand.Commands.Keys){
            msg += "    " + method + "\n";
        }

        game.Send(msg);

    }

    [GameCommand("View some general stats about the world.")]
    public static void WorldStatus(Player p, GameHub game, string args)
    {
        game.Send("World created: " + World.instance.worldCreationDate.ToString());
    }
}
