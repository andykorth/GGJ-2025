using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

public class Player : IShortLine
{
	public string name;
	public string uuid;
	public int cash;
	public int tutorialStep = 0;

	public List<string> exploredSiteUUIDs;
	public List<Ship> ships;
	internal List<int> relicIDs;
	public DateTime lastActivity;
	public DateTime created;
	public List<Message> messages;
	public List<Building> buildings;

	public string? currentResearch;
	public float currentResearchProgress;

	[NonSerialized]
	internal ISingleClientProxy client;
	[NonSerialized]
	internal string? connectionID;

	[NonSerialized]
	internal Func<string, bool>? captivePrompt;
	[NonSerialized]
	internal string? captivePromptMsg;

	[NonSerialized]
	public Context currentContext;

	public Player()
	{
		// default constructor for newtonsoft
	}

	internal void Migrate()
	{
		if (relicIDs == null) relicIDs = new List<int>();
		if (messages == null) messages = new List<Message>();
		if (buildings == null) buildings = new List<Building>();
		if (ships == null) ships = new List<Ship>();

		foreach(var ship in ships){
			ship.Migrate();
		}
	}

	public Player(string name) : this()
	{
		this.name = name;
		cash = 10000;
		tutorialStep = 0;
		exploredSiteUUIDs = new List<string>();
		relicIDs = new List<int>();
		ships = new List<Ship>();
		uuid = System.Guid.NewGuid().ToString();
		created = DateTime.Now;
		lastActivity = DateTime.Now;

		// start with two ships!
		Ship s = new Ship();
		s.Init(World.instance.GetShipDesign("Pioneer"));
		ships.Add(s);

		s = new Ship();
		s.Init(World.instance.GetShipDesign("Pioneer"));
		ships.Add(s);

		currentResearch = null;
		currentResearchProgress = 0f;
		Migrate();
	}

    public void SetContext(Context c, GameUpdateService service){

        this.currentContext = c;
        string[] commands = c.Commands.Keys.ToArray();
        service.SendCommandList(this, commands, c.Name );

		Log.Info($"[{name}] swap to context: {currentContext.Name})");

		c.EnterContext(this, service);
    }

	public List<ExploredSite> GetExploredSites()
	{
		List<ExploredSite> exploredSites = new List<ExploredSite>();
		foreach (var uuid in this.exploredSiteUUIDs)
		{
			exploredSites.Add(World.instance.GetSite(uuid)!);
		}
		return exploredSites;
	}

	public List<Relic> GetRelics()
	{
		List<Relic> list = new List<Relic>();
		foreach (var relicIndex in this.relicIDs)
		{
			list.Add(new Relic(){id = relicIndex});
		}
		return list;
	}

	internal void Send(string output)
	{
		client.SendAsync("ReceiveLine", output);
	}

	public int UnreadMessages()
	{
		if (messages == null) messages = new List<Message>();
		int unreadCount = this.messages.Count(m => m != null && !m.read.HasValue);
		return unreadCount;
	}

    public string ShortLine(Player p, int index)
    {
		TimeSpan duration = DateTime.Now - this.lastActivity;
		return $"{this.name} - {Ascii.TimeAgo(duration)}\n";
    }

    internal Building? FindBuilding(string targetUUID)
    {
        return buildings.Find(b => b.uuid == targetUUID);
    }

    public void Invoke(GameUpdateService game, string message)
    {
		// run the general command entry.
		var m = message.Split(" ", 2, StringSplitOptions.TrimEntries);
		string command = m[0];
		string args = m.Length > 1 ? m[1] : "";

		Log.Info($"[{name}] ({currentContext.Name}): [{command}] [{args}]");

        if (currentContext.Commands.TryGetValue(command.ToLower(), out var method))
        {
            try {
                game.Send(this, $"[magenta]>{command}[/magenta] {Ascii.WrapColor(args, "DarkMagenta")}");
                method.Invoke(null, [this, game, args]);
            }
            catch (Exception ex) {
                if(ex.InnerException != null){
                    game.Send(this, $"Error executing command [{command}]: [red]{ex.InnerException!.Message}[/red]");
                    game.Send(this, ex!.InnerException!.StackTrace!);
                    Log.Error(ex.InnerException.ToString());
                }else{
                    game.Send(this, $"Error executing command [{command}]: [red]{ex.Message}[/red]");
                    game.Send(this, ex.StackTrace!);
                    Log.Error(ex.ToString());
                }
            }
            return;
        }

        game.Send(this, $"Command [red]{command}[/red] not recognized. Type [salmon]help[/salmon]");
    }

}

public class Relic : IShortLine
{
	public int id;

	public string ShortLine(Player p, int index = -1)
	{
		string showIndex = index < 0 ? "" : index + ")";
		return $"   {showIndex} [cyan]{GetName()}[/cyan]\n";
	}

	public string GetName(){
		return Ascii.relicNames[id];
	}
}

public class Ship : IShortLine
{
	public string? name;
	public string uuid;
	public ShipDesign shipDesign;

	[JsonIgnore]
	public ExploredSite lastLocation{
		set{
			lastLocationSiteUUID = value.uuid;
		}
		get {
			return World.instance.GetSite(lastLocationSiteUUID)!;
		}
	}
	public string lastLocationSiteUUID;

	public ShipMission shipMission = ShipMission.Idle;
	public float condition = 1.0f;
	// this isn't used for the timer, but it's here so we can show it.
	public long arrivalTime = 0;


	public string ShortLine(Player p, int index = -1)
	{
		string showIndex = index < 0 ? "" : index + ")";
		string mission = shipMission.ToString();
		if (shipMission == ShipMission.Exploring)
		{
			mission += $" ({arrivalTime - World.instance.ticks}s)";
		}
		return $"   {showIndex} {GetName()} - Health: {(int)(condition * 100)}%, {mission}\n";
	}

	internal string LongLine()
	{
		string s = "";
		if (shipMission == ShipMission.Idle)
		{
			s += $"      Current Location: {(lastLocation == null ? "Station" : lastLocation.name)} \n";
		}
		if (shipMission == ShipMission.Exploring)
		{
			s += $"      Exploring... Arrives in {(arrivalTime - World.instance.ticks)}s \n";
		}
		s += "      Ship Design Speed: 1.0                     Actual Speed: 1.0\n";
		return s;
	}

	internal void Init(ShipDesign shipDesign)
	{
		this.uuid = System.Guid.NewGuid().ToString();
		this.shipDesign = shipDesign;
		this.shipMission = ShipMission.Idle;
		this.condition = 1.0f;
	}

	public void Migrate(){
		if(shipDesign == null){
			shipDesign = World.instance.GetShipDesign("Pioneer");
		}
	}

	public string GetName()
	{

		return name ?? shipDesign.name;
	}

	public enum ShipMission
	{
		Idle,
		Exploring
	}

}

public class ShipDesign
{
	public string uuid;
	public string name;
	public string description;

	internal static ShipDesign BasicExplorer()
	{
		ShipDesign sd = new ShipDesign();
		sd.uuid = System.Guid.NewGuid().ToString();
		sd.name = "Pioneer";
		sd.description = "Basic Exploration Ship";
		return sd;
	}
}

public class Message : IShortLine
{
	public DateTime sent;
	public DateTime? read;
	public MessageType type;
	public string contents;
	public string fromPlayerUUID;
	public string? invitationSiteUUID;

	public enum MessageType
	{
		TextMail, Invitation,
	}

	public Message()
	{
		// newtonstoft empty
	}

	public Message(Player fromPlayer, MessageType type, string contents)
	{
		this.sent = DateTime.Now;
		this.type = type;
		this.contents = contents;
		this.fromPlayerUUID = fromPlayer.uuid;
		this.read = null;
	}

    public string ShortLine(Player p, int index)
    {
		DateTime now = DateTime.Now;
		// Determine if the message is read or unread
		string status = this.read.HasValue ? "<read>" : "[cyan]<unread>[/cyan]";
		string from = World.instance.GetPlayer(this.fromPlayerUUID)!.name;
		// Display the message details
		return $"[{Ascii.TimeAgo(now - this.sent)}] {status} - {this.type} from {from}: {Ascii.Shorten(this.contents, 20)}";

    }
}