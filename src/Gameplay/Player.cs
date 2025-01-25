using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

public class Player
{
	public string name;
	public int cash;
	public bool hasFinishedTutorial = false;

	public List<string> exploredSiteUUIDs;
	public List<Ship> ships;	

	public string? currentResearch;
	public float currentResearchProgress;


	public Player(string name){
		this.name = name;
		cash = 10000;
		hasFinishedTutorial = false;
		exploredSiteUUIDs = new List<string>();
		ships = new List<Ship>();
		Ship s = new Ship();
		s.shipDesign = World.instance.GetShipDesign("Pioneer");
		ships.Add(n);

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
	public ShipDesign shipDesign;
	public ExploredSite lastLocation;
	public ShipMission shipMission = ShipMission.Idle;


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