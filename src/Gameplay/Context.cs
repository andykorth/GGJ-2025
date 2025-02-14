
using System.Reflection;
using Microsoft.AspNetCore.SignalR;

public abstract class Context
{
    public abstract string Name { get; }
    public virtual bool rootContext { get { return false;}  }
    public abstract void EnterContext(Player p, GameUpdateService service);

    internal Dictionary<string, MethodInfo> Commands { get; } = new();
    internal Dictionary<string, GameCommandAttribute> HelpAttrs { get; } = new();

    internal virtual void Invoke(Player p, GameUpdateService game, string command, string args)
    {
        if (this.Commands.TryGetValue(command.ToLower(), out var method))
        {
            try {
                game.Send(p, $"[magenta]>{command}[/magenta] {Ascii.WrapColor(args, "DarkMagenta")}");
                method.Invoke(null, [p, game, args]);
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

        game.Send(p, $"Command [red]{command}[/red] not recognized in [yellow]{p.currentContext.Name}[/yellow]. Type [salmon]help[/salmon]");
    }

    [GameCommand("See this help message for your current context.", true)]
    public static void Help(Player p, GameUpdateService game, string args)
    {
        bool showAll = args.Contains("all");
        var c = p.currentContext;
        
        int hiddenCount = 0;
        string list = "";
        foreach(var method in c.Commands.Keys){
            var a = c.HelpAttrs[method + ""];
            if(a.normallyHidden) hiddenCount += 1;
            if(showAll || !a.normallyHidden)
                list += $"    [salmon]{method}[/salmon] - {a.helpText} \n";
        }

        string hidden = "";
        if(hiddenCount > 1){
            hidden = $"(To see {hiddenCount} hidden commands, use [salmon]help all[/salmon])";
        }

        string msg = "";
        msg += $"[yellow]{c.Name} Menu:[/yellow]\n";
        msg += $"These are the commands in your current context [yellow]({c.Name})[/yellow] you can type. {hidden}\n";
        msg += list;

        game.Send(p, msg);
    }

    [GameCommand("Exit current context.", true)]
    public static void Exit(Player p, GameUpdateService game, string args)
    {
        p.SetContext<MainContext>();
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

    internal static string ShowResearchBoosts(int showMax, Player p, int start)
    {
        var list = p.GetResearchProjects();
        return ShowList(list.Cast<IShortLine>().ToList(), "Research Boosts", "---", showMax, p, start);
    }

    internal static int PullIntArg(Player p, ref string args, bool optional = false){
        string indexS = PullArg(ref args);
        int index = -1;
        if(int.TryParse(indexS, out index)){
            return index;
        }else{
            if(!optional) p.Send( $"You were supposed to provide a number, not [{indexS}]!");
            return -1;
        }
    }

    internal static T? PullIndexArg<T>(Player p, GameUpdateService game, ref string args, List<T> list){
        string indexS = PullArg(ref args);
        int index = -1;
        if(int.TryParse(indexS, out index)){
            if(index < 0 || index >= list.Count){
                game.Send(p, $"Invalid range: [{indexS}] is not between {0} and {list.Count-1}, inclusive.");
                return default(T);
            }
            return list[index];
        }else{
            game.Send(p, $"Bad index specified: [{indexS}] That should have been a number between {0} and {list.Count-1}, inclusive.");
            return default(T);
        }
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

    internal (List<string>,List<string>) GetCommandList()
    {
        List<string> commands = [];
		List<string> commandHelps = [];
        Context c = this;

        foreach(var x in c.Commands.Keys){
			if(!c.HelpAttrs[x].normallyHidden){
				commands.Add(x);
				commandHelps.Add(c.HelpAttrs[x].helpText);
			}
		}
		if(!c.rootContext){
			commands.Add("exit");
			commandHelps.Add("Exit the current context and return to the MainMenu.");
		}
        return (commands, commandHelps);
    }
}
