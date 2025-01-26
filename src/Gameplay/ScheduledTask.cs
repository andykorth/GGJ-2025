
using System.Xml;
using Microsoft.AspNetCore.SignalR;

public class ScheduledTask
{
    public long completedOnTick;

    public string playerUUID;
    public string targetUUID;
    public ScheduledAction task;
    public int seed;
    public string string1;
    public BuildingType buildingType;

    public ScheduledTask(){
        // for json.net
    }

    public ScheduledTask(int duration, Player? p, Ship? s, ScheduledAction task)
    {
        // store a seed so the results are reproducable
        seed = new Random().Next();
        long now = World.instance.ticks;
        completedOnTick = now + (long) duration;

        if(p!=null) this.playerUUID = p.uuid;
        if(s!=null) this.targetUUID = s.uuid;
        this.task = task;
    }
    
    public ScheduledTask(int duration, Player? p, ExploredSite? s, ScheduledAction task)
    {
        // store a seed so the results are reproducable
        seed = new Random().Next();
        long now = World.instance.ticks;
        completedOnTick = now + (long) duration;

        if(p!=null) this.playerUUID = p.uuid;
        if(s!=null) this.targetUUID = s.uuid;
        this.task = task;
    }
    
    public void InvokeAction(GameUpdateService service)
    {
        Player? p = null;
        if(this.playerUUID != null)
            p = World.instance.GetPlayer(this.playerUUID);


        if(task == ScheduledAction.ExplorationMission)
        {
            Ship? s = null;
            if (p != null && targetUUID != null)
                s = World.instance.GetShip(p, this.targetUUID);
            ExploreMission(service, p!, s!);
        }
        if(task == ScheduledAction.SiteDevelopmentMission)
        {
            ExploredSite? s = null;
            if (p != null && targetUUID != null)
                s = World.instance.GetSite(this.targetUUID);
            DevelopmentMission(service, p!, s!);
        }
        if(task == ScheduledAction.SiteConstruction)
        {
            ExploredSite? s = null;
            if (p != null && targetUUID != null)
                s = World.instance.GetSite(this.targetUUID);
            SiteConstruction(service, p!, s!);
        }
    }

    private void ExploreMission(GameUpdateService service, Player p, Ship s)
    {
        // find a new planet.
        Random r = new Random(seed);
        float damage = Math.Max(0, ((float)r.NextDouble()) - 0.65f) * 0.6f;

        string otherPlayers = $"`{s!.GetName()}` from {p!.name} has discovered a new planet!\n";
        string output = $"`{s!.GetName()}` from {p!.name} is approaching a new planet!\n";
        if (damage > 0.1)
        {
            output += "It was somewhat damaged by micrometeorite impacts.\n";
        }
        else if (damage > 0)
        {
            output += "Solar radiation has caused slight hull ablation.\n";
        }
        s.condition -= damage;
        output += $" Ship Condition: {(int)(s.condition * 100)}%";

        float planetClass = (float)r.NextDouble();
        int index = (int)(Ascii.planetNames.Count() * planetClass);

        ExploredSite newSite = new ExploredSite();
        string name = Ascii.planetNames[index];
        newSite.Init(name, p.uuid, planetClass);

        otherPlayers += $"{name}: {newSite.ClassString()}\n";

        float relic = (float)r.NextDouble();

        output += $"\n \nDiscovered {name}!\n \n";

        if (newSite.GoldilocksClass())
        {
            output += $"{name} is a goldilocks zone planet, capable of supporting T1 or T2 constructions.\n";
        }
        else
        if (newSite.StandardClass())
        {
            output += $"{name} is a habitable planet, capable of supporting T2 constructions.\n";
        }
        else
        {
            output += $"Unfortunately, {name} is not habitable, and does not support construction.\n";
            relic += 0.1f;
        }
        if (relic > 0.5)
        {

            int relicIndex = (int)(Ascii.relicNames.Count() * ((float)r.NextDouble()));
            string relicName = Ascii.relicNames[relicIndex];
            p.relicIDs.Add(relicIndex);

            output += $" \n{s!.GetName()} has found a relic on {name}!\n";
            output += $"It is a [cyan]{relicName}[/cyan]! Perhaps we can research it.\n";

            otherPlayers += $"{p!.name} has uncovered a relic!\n";
        }

        // update ship status.
        s.arrivalTime = 0;
        s.shipMission = Ship.ShipMission.Idle;
        s.lastLocation = newSite;

        // save the new site!
        World.instance.allSites.Add(newSite);
        p.exploredSiteUUIDs.Add(newSite.uuid);

        // TODO check if ship is wrecked.

        service.SendExcept(p.connectionID, Ascii.Box(otherPlayers));
        service.SendTo(p.connectionID, Ascii.Box(output));
    }

    private void DevelopmentMission(GameUpdateService service, Player p, ExploredSite s)
    {
        string projectName = this.string1;
        
        s.population += 10;
        
        string otherPlayers = $"{p.name} has constructed a [cyan]{projectName}[/cyan]\n";
        otherPlayers += $"on {s.name}. The population is now {s.population}k";
        service.SendExcept(p.connectionID, Ascii.Box(otherPlayers));

        string output = $"It is a glorious day on {s.name}!\n";
        output += $"The [cyan]{projectName}[/cyan] you funded is finally complete.\n";
        output += $"New pioneers stream to the planet, and the population is now {s.population}.\n";
        output += $"These new people will soon be looking for more retail shopping options.\n";

        service.SendTo(p.connectionID, Ascii.Box(output));
    }

    private void SiteConstruction(GameUpdateService service, Player p, ExploredSite s)
    {
        string projectName = this.buildingType.ToString();
        
        Building b = new Building();
        b.Init(s, p, buildingType);
        p.buildings.Add(b);

        // string otherPlayers = $"{p.name} has constructed a [cyan]{projectName}[/cyan]\n";
        // otherPlayers += $"on {s.name}. The population is now {s.population}k";
        // service.SendExcept(p.connectionID, Ascii.Box(otherPlayers));

        string output = "";
        output += $"Your [cyan]{projectName}[/cyan] is complete on {s.name}!\n";

        service.SendTo(p.connectionID, Ascii.Box(output));
    }
}

public enum ScheduledAction{
    ExplorationMission, SiteDevelopmentMission, SiteConstruction,
}