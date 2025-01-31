
public class ShipContext : Context {

    public override string Name => "Ship";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        p.Send("[yellow]Ship Menu:[/yellow]");
        string s = Context.ShowShips(20, p, 0);
        game.Send(p, s);
    }

    [GameCommand("explore 0: Send your first ship to explore")]
    public static void Explore(Player p, GameUpdateService game, string args)
    {
        Ship? s = PullIndexArg<Ship>(p, game, ref args, p.ships);
        if(s != null){
            string output = "Begin exploration mission with:\n";
            output += s.ShortLine(p, -1);
            output += s.LongLine();

            if(s.shipMission == global::Ship.ShipMission.Idle){

                int ticks = 200 / World.instance.timescale;
                output += $" Exploration mission will take {ticks} seconds.";

                game.Send(p, output);
                p.SetCaptiveYNPrompt( "Do you want to start this mission? (y/n)", (bool response) => {
                    if(response)
                        StartExploration(ticks, p, s);
                    else
                        game.Send(p, "Exploration canceled.");
                });
            }else{
                output += $" Ship is already on a mission!";
                game.Send(p, output);
            }
        }

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
    public static void Rename(Player p, GameUpdateService game, string args)
    {
        Ship? ship = PullIndexArg<Ship>(p, game, ref args, p.ships);
        if(ship != null){
            game.Send(p, $"Ship {ship.name} renamed to {args}");
            ship.name = args;
        }
    }   

    [GameCommand("view 0: View details of the specified ship.")]
    public static void View(Player p, GameUpdateService game, string args)
    {
        Ship? ship = PullIndexArg<Ship>(p, game, ref args, p.ships);
        if(ship != null){
            game.Send(p, ship.ShortLine(p, -1));
            game.Send(p, ship.LongLine());
        }
    }

    private static void Repair(Player p, GameUpdateService game, string args)
    {
        throw new NotImplementedException();
    }
}
