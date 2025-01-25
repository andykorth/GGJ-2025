
public class ScheduledTask
{
    public long completedOnTick;

    public string playerUUID;
    public string shipUUID;
    public ScheduledAction task;
    public int seed;

    public ScheduledTask(int duration, Player? p, Ship? s, ScheduledAction task)
    {
        // store a seed so the results are reproducable
        seed = new Random().Next();
        long now = World.instance.ticks;
        completedOnTick = now + (long) duration;

        if(p!=null) this.playerUUID = p.uuid;
        if(s!=null) this.shipUUID = s.uuid;
        this.task = task;
    }
    
    public void InvokeAction(){
        Player? p = null;
        if(this.playerUUID != null)
            p = World.instance.FindPlayer(this.playerUUID);
        Ship? s = null;
        if(p != null && shipUUID != null)
            s = World.instance.FindShip(p, this.shipUUID);

        if(task == ScheduledAction.ExplorationMission){
            // find a new planet.
            string output = "   /-------------------\n";
            output += $"   | {s!.name} from {p!.name} is approaching a new planet!\n";
            output += "   \\-------------------\n";

            GameHub.instance.SendAll(output);
        }
    }
}

public enum ScheduledAction{
    ExplorationMission
}