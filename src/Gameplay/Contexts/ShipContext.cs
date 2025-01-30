
public class ShipContext : Context {

    public override string Name => "Ship";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        string s = Context.ShowShips(20, p, 0);
        s += Ascii.Header("Ship Commands", 40);

        game.Send(p, s);
    }

    [GameCommand("explore 0: Send your first ship to explore")]
    public static void ShipExplore(Player p, GameUpdateService game, string args)
    {
        int index = 0;
        if(int.TryParse(args, out index)){
            string output = "Begin exploration mission with:\n";
            Ship s = p.ships[index];
            output += s.ShortLine(p, -1);
            output += s.LongLine();

            if(s.shipMission == global::Ship.ShipMission.Idle){

                int ticks = 200 / World.instance.timescale;
                output += $" Exploration mission will take {ticks} seconds.";

                game.Send(p, output);
                game.SetCaptiveYNPrompt(p, "Do you want to start this mission? (y/n)", (bool response) => {
                    if(response)
                        StartExploration(ticks, p, s);
                    else
                        game.Send(p, "Exploration canceled.");
                });
            }else{
                output += $" Ship is already on a mission!";
                game.Send(p, output);
            }
            return;

        }
        game.Send(p, $"Invalid ship exploration args [{args}]");

    }


    private static void StartExploration(int duration, Player p, Ship s)
    {
        s.shipMission = global::Ship.ShipMission.Exploring;
        s.arrivalTime = duration + World.instance.ticks;
        ScheduledTask st = new ScheduledTask(duration, p, s, ScheduledAction.ExplorationMission);
        World.instance.Schedule(st);

        string output = $"{s.GetName()} departs for the stars.\n";
        output += $"Hopefully their efforts will be fruitful.\n";
        output += $"We expect to hear from them in {duration}s.\n";

        p.Send(Ascii.Box(output, "blue"));
    }

    [GameCommand("rename 0 Big Bertha: Rename your first ship to 'Big Bertha'")]
    public static void ShipRename(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            Ship ship = p.ships[index];
            ship.name = args;
            game.Send(p, $"Ship {index} renamed to {args}");
        }else{
            game.Send(p, $"Bad index for rename [{indexS}]");
        }
    }   

    [GameCommand("view 0: View details of the specified ship.")]
    public static void ShipView(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            Ship ship = p.ships[index];

            game.Send(p, ship.ShortLine(p, -1));
            game.Send(p, ship.LongLine());
        }else{
            game.Send(p, $"Bad index for ship viewing [{indexS}]");
        }
    }

    private static void ShipRepair(Player p, GameUpdateService game, string args)
    {
        throw new NotImplementedException();
    }
}
