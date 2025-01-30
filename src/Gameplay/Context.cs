
using System.Reflection;
using Microsoft.AspNetCore.SignalR;

public abstract class Context
{
    public abstract string Name { get; }
    public virtual bool rootContext { get { return false;}  }
    public abstract void EnterContext(Player p, GameUpdateService service);

    internal Dictionary<string, MethodInfo> Commands { get; } = new();
    internal Dictionary<string, GameCommandAttribute> HelpAttrs { get; } = new();

    [GameCommand("See this help message for your current context.", true)]
    public static void Help(Player p, GameUpdateService game, string args)
    {
        bool showAll = args.Contains("all");

        string msg = $"These are the commands in your current context ({p.currentContext.Name}) you can type: (To see them all, try [salmon]help all[/salmon])\n";
        foreach(var method in p.currentContext.Commands.Keys){
            var a = p.currentContext.HelpAttrs[method + ""];
            if(showAll || !a.normallyHidden)
                msg += $"    [salmon]{method}[/salmon] - {a.helpText} \n";
        }

        game.Send(p, msg);
    }

    internal static string ShowShips(int showMax, Player p, int start)
    {
        var list = p.ships;
        return ShowList(list.Cast<IShortLine>().ToList(), "Ships", "ship", showMax, p, start);
    }

    internal static string ShowList(List<IShortLine> list, string title, string command, int showMax, Player p, int start, int headerWidth = 40)
    {
        string output = Ascii.Header($"{title} [{list.Count}]", headerWidth);
        int count = 0;
        
        foreach (var item in list){
            if(count < start) {
                count += 1;
                continue;
            }
            if(count > showMax){
                output += $"     ... {count-showMax} more entries. (type [magenta]{command} {count}[/magenta] to start at that item)\n";
                break;
            }
            output += item.ShortLine(p, count);
            count += 1;
        }
        return output;
    }

    internal static string ShowSites(int showMax, Player p, int start)
    {
        var list = p.GetExploredSites();
        return ShowList(list.Cast<IShortLine>().ToList(), "Discovered Sites", "site", showMax, p, start);
    }

    internal static string ShowRelics(int showMax, Player p, int start)
    {
        var list = p.GetRelics();
        return ShowList(list.Cast<IShortLine>().ToList(), "Unresearched Relics", "---", showMax, p, start);
    }

    internal static string PullArg(ref string args){
        var split = args.Split(" ", 2, StringSplitOptions.TrimEntries);
        if(split.Length > 1){
            args = split[1];
        }else{
            args = "";
        }
        return split[0];
    }

}
