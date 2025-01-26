using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

public class Player
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

	public string? currentResearch;
	public float currentResearchProgress;

	[NonSerialized]
    internal ISingleClientProxy client;
	[NonSerialized]
    internal string connectionID;

	[NonSerialized]
    internal Func<string, bool>? captivePrompt;
	[NonSerialized]
    internal string? captivePromptMsg;

    public Player(){
		// default constructor for newtonsoft
		// use to migrate old saves.
		if(relicIDs == null) relicIDs = new List<int>();
		if(messages == null) messages = new List<Message>();
	}

	public Player(string name){
		this.name = name;
		cash = 10000;
		tutorialStep = 0;
		exploredSiteUUIDs = new List<string>();
		ships = new List<Ship>();
		relicIDs = new List<int>();
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
		
	}

    public List<ExploredSite> GetExploredSites()
    {
        List<ExploredSite> exploredSites= new List<ExploredSite>();
		foreach(var uuid in this.exploredSiteUUIDs){
			exploredSites.Add(World.instance.GetSite(uuid)!);
		}
		return exploredSites;
    }

    internal void Send(string output)
    {
        client.SendAsync("ReceiveLine", output);
    }

    public int UnreadMessages()
    {
		int unreadCount = this.messages.Count(m => !m.read.HasValue);
		return unreadCount;
    }
}

public class ExploredSite {
	public string uuid;
	public string name;
	public string discoveredByPlayerUUID;
	public DateTime discoveredDate;
	public float planetClass;
	public int population = 0;

	public float DevelopmentPriceFactor(){
		return (1f - planetClass) * 0.4f + 0.8f;
	}

    internal bool GoldilocksClass()
    {
        return planetClass > 0.8;
    }

    internal bool StandardClass()
    {
        return planetClass > 0.5;
    }

    internal void Init(string name, string discoveredBy, float planetClass)
    {
        this.uuid = System.Guid.NewGuid().ToString();
		this.name = name;
		this.discoveredByPlayerUUID = discoveredBy;
		this.discoveredDate = DateTime.Now;
		this.planetClass = planetClass;
    }

	public string ClassString(){
		return GoldilocksClass() ? "Goldilocks Class" : (StandardClass() ? "Habitable" : "Uninhabitable");
	}

    internal string ShortLine(int index = -1)
    {
		string developmentIcon = "<[grey]-[/grey]>";
		if(population > 0){
			developmentIcon = "<[white]-[/white]>";
		}
		if(population > 12){
			developmentIcon = "<[cyan]*[/cyan]>";
		}
		if(population > 30){
			developmentIcon = "<[orchid]x[/orchid]>";
		}
		if(population > 60){
			developmentIcon = "<[yellow]#[/yellow]>";
		}
		if(population > 100){
			developmentIcon = "<[chartreuse]@[/chartreuse]>";
		}
		if(population > 150){
			developmentIcon = "<[RebeccaPurple]&[/RebeccaPurple]>";
		}
		string classMessage = ClassString();
		string showIndex = index < 0 ? "" : index + ")";
		return $"   {showIndex} {developmentIcon} {name} - {classMessage}\n";
    }

    internal string LongLine()
    {
		int playerCount = 0;
		foreach(Player p in World.instance.allPlayers){
			if(p.exploredSiteUUIDs.Contains(this.uuid)){
				playerCount += 1;
			}
		}
		return $"   Population: {population}k\n   Players: {playerCount}\n   Discovered by {World.instance.GetPlayer(discoveredByPlayerUUID)!.name} on {discoveredDate.ToString()}\n";
    }
}

public class Ship{
	public string? name;
	public string uuid;
	public ShipDesign shipDesign;
	public ExploredSite lastLocation;
	public ShipMission shipMission = ShipMission.Idle;
    public float condition = 1.0f;
	// this isn't used for the timer, but it's here so we can show it.
	public long arrivalTime = 0;


    internal string ShortLine(int index = -1)
    {
		string showIndex = index < 0 ? "" : index + ")";
		string mission = shipMission.ToString();
		if(shipMission  == ShipMission.Exploring){
			mission += $" ({arrivalTime - World.instance.ticks}s)";
		}
		return $"   {showIndex} {GetName()} - Health: {(int)(condition*100)}%, {mission}\n";
    }

    internal string LongLine()
    {
		string s = "";
		if(shipMission == ShipMission.Idle){
			s += $"      Current Location: {(lastLocation == null ? "Station" : lastLocation.name) } \n";
		}
		if(shipMission == ShipMission.Exploring){
			s += $"      Exploring... Arrives in {(arrivalTime - World.instance.ticks) }s \n";
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

    public string GetName()
    {
        return name ?? shipDesign.name;
    }

    public enum ShipMission {
		Idle,
        Exploring
    }

}

public class ShipDesign{
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

public class Message{
	public DateTime sent;
	public DateTime? read;
	public MessageType type;
	public string contents;
	public string fromPlayerUUID;
	public string? invitationSiteUUID;

	public enum MessageType {
		TextMail, Invitation,
	}

	public Message(){
		// newtonstoft empty
	}

	public Message(Player fromPlayer, MessageType type, string contents){
		this.sent = DateTime.Now;
		this.type = type;
		this.contents = contents;
		this.fromPlayerUUID = fromPlayer.uuid;
		this.read = null;
	}
}