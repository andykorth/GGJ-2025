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

	public string? currentResearch;
	public float currentResearchProgress;

	[NonSerialized]
    internal HubCallerContext connection;
	[NonSerialized]
    internal Func<string, bool>? captivePrompt;
	[NonSerialized]
    internal string? captivePromptMsg;

    public Player(){
		// default constructor for newtonsoft
	}

	public Player(string name){
		this.name = name;
		cash = 10000;
		tutorialStep = 0;
		exploredSiteUUIDs = new List<string>();
		ships = new List<Ship>();
		uuid = System.Guid.NewGuid().ToString();
		
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
	
}


public class ExploredSite{
	public string uuid;
	public string name;
	public string discoveredBy;
	public DateTime discoveredDate;

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


    internal string ShortLine(int index)
    {
		string showIndex = index < 0 ? "" : index + ")";
		string n = name ?? shipDesign.name;
		return $"   {showIndex} {n} - Status: {((int)(condition*100))}%, {shipMission}\n";
    }

    internal string LongLine()
    {
		string s = "";
		if(shipMission == ShipMission.Idle){
			s += $"      Current Location: {(lastLocation == null ? "Station" : lastLocation.name) } \n";
		}
		if(shipMission == ShipMission.Exploring){
			s += $"      Exploring... Arrives in {(arrivalTime - World.instance.ticks) } \n";
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