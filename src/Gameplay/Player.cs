using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

public class Player
{
	public string name;
	public int cash;
	public int tutorialStep = 0;

	public List<string> exploredSiteUUIDs;
	public List<Ship> ships;	

	public string? currentResearch;
	public float currentResearchProgress;

	[NonSerialized]
    internal HubCallerContext connection;

    public Player(){
		// default constructor for newtonsoft
	}

	public Player(string name){
		this.name = name;
		cash = 10000;
		tutorialStep = 0;
		exploredSiteUUIDs = new List<string>();
		ships = new List<Ship>();
		
		// start with two ships!
		Ship s = new Ship();
		s.shipDesign = World.instance.GetShipDesign("Pioneer");
		ships.Add(s);
		
		s = new Ship();
		s.shipDesign = World.instance.GetShipDesign("Pioneer");
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
	public ShipDesign shipDesign;
	public ExploredSite lastLocation;
	public ShipMission shipMission = ShipMission.Idle;
    public float condition = 1.0f;


    internal string ShortLine(int index)
    {
		string showIndex = index <= 0 ? "" : index + ")";
		string n = name ?? shipDesign.name;
		return $"   {showIndex} {n} - Status: {((int)(condition*100))}%, {shipMission}\n";
    }

    internal string LongLine()
    {
		return $"      Current Location: {(lastLocation == null ? "Station" : lastLocation.name) } \n";
    }
    public enum ShipMission {
		Idle
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