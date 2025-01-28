
using System.Xml;
using Microsoft.AspNetCore.SignalR;

public class ScheduledTask
{
    public string uuid;
    public long completedOnTick;
    public int originalDuration;

    public string playerUUID;
    public string targetUUID;
    public ScheduledAction task;
    public int seed;
    public string string1;
    public BuildingType buildingType;
    public float materialAmount;
    public string materialUUID;

    public ScheduledTask(){
        // for json.net
    }

    public ScheduledTask(int duration, Player? p, ScheduledAction task){
        this.task = task;
        this.uuid = System.Guid.NewGuid().ToString();
        if(p!=null) this.playerUUID = p.uuid;
        long now = World.instance.ticks;
        this.originalDuration = duration;
        this.completedOnTick = now + (long) duration;

    }

    public ScheduledTask(int duration, Player? p, Ship? s, ScheduledAction task) : this(duration, p, task)
    {
        // store a seed so the results are reproducable
        seed = new Random().Next();

        if(s!=null) this.targetUUID = s.uuid;
    }
    
    public ScheduledTask(int duration, Player? p, ExploredSite? s, ScheduledAction task) : this(duration, p, task)
    {
        // store a seed so the results are reproducable
        seed = new Random().Next();

        if(s!=null) this.targetUUID = s.uuid;
    }

    public ScheduledTask(int duration, Player? p, Building? building, ScheduledAction task) : this(duration, p, task)
    {
        if(building!=null) this.targetUUID = building.uuid;
    }

    private void Reschedule(){
        this.completedOnTick = this.originalDuration + World.instance.ticks;
        World.instance.Schedule(this);
    }

    public long TicksRemaining(){
        return this.completedOnTick - World.instance.ticks;
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
        else if(task == ScheduledAction.SiteDevelopmentMission)
        {
            ExploredSite? s = null;
            if (p != null && targetUUID != null)
                s = World.instance.GetSite(this.targetUUID);
            DevelopmentMission(service, p!, s!);
        }
        else if(task == ScheduledAction.SiteConstruction)
        {
            ExploredSite? s = null;
            if (p != null && targetUUID != null)
                s = World.instance.GetSite(this.targetUUID);
            SiteConstruction(service, p!, s!);
        }
        else if(task == ScheduledAction.Production)
        {
            Building? s = null;
            if (p != null && targetUUID != null)
                s = p.FindBuilding(this.targetUUID);
            Production(service, p!, s!);
        }else{
            Log.Error("Unimplemented scheduled task action: " + task);
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
        int matchingNames = World.instance.allSites.FindAll(s => s.name.StartsWith(name)).Count();
        if(matchingNames > 0){
            name = $"{name} {Ascii.ToRomanNumeral(matchingNames)}";
        }

        newSite.Init(name, p.uuid, planetClass);

        otherPlayers += $"{name}: {newSite.ClassString()}\n";

        float relic = (float)r.NextDouble();

        output += $"\n \nDiscovered {name}!\n \n";

        if (newSite.GoldilocksClass())
        {
            output += $"{name} is a goldilocks zone planet, capable of supporting many happy citizens.\n";
        }
        else
        if (newSite.StandardClass())
        {
            output += $"{name} is a habitable planet, although it will be a hardscrabble life.\n";
        }
        else
        {
            output += $"Unfortunately, {name} is a tough airless rock. Colonists will need to live in pods.\n";
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

        service.SendExcept(p.connectionID!, Ascii.Box(otherPlayers));
        service.Send(p, Ascii.Box(output, "blue"));
    }

    private void DevelopmentMission(GameUpdateService service, Player p, ExploredSite s)
    {
        string projectName = this.string1;
        
        s.population += 10;
        
        string otherPlayers = $"{p.name} has constructed a [cyan]{projectName}[/cyan]\n";
        otherPlayers += $"on {s.name}. The population is now {s.population}k";
        service.SendExcept(p.connectionID!, Ascii.Box(otherPlayers));

        string output = $"It is a glorious day on {s.name}!\n";
        output += $"The [cyan]{projectName}[/cyan] you funded is finally complete.\n";
        output += $"New pioneers stream to the planet, and the population is now {s.population}k.\n";
        output += $"These new people will soon be looking for more retail shopping options.\n";

        service.Send(p, Ascii.Box(output, "yellow"));
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
        output += $"(start production using the [salmon]prod[/salmon] command)\n";

        service.Send(p, Ascii.Box(output, "yellow"));
    }

    private void Production(GameUpdateService service, Player player, Building building)
    {
		var site = World.instance.GetSite(building.siteUUID);
		string siteName = Ascii.WrapColor(site!.name, site!.SiteColor());

        Material m = World.instance.allMats.Find(m => m.uuid == this.materialUUID)!;
        float quantity = this.materialAmount;

        string s = $"-> Your {building.GetName()} on {siteName} has produced {quantity:0.00} {m.name} \n";
        service.Send(player, s);

        // requeue the task!
        Reschedule();
    }



}

public enum ScheduledAction{
    ExplorationMission, SiteDevelopmentMission, SiteConstruction, Production
}