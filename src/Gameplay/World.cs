using System;
using System.Collections.Concurrent;
using System.IO.IsolatedStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

public class World
{
	// Everything you want to save or serialize is stored on the world.
	public static World instance;

	public DateTime worldCreationDate;
	public List<Player> allPlayers;

	public List<ExploredSite> allSites;
	public List<ShipDesign> allShipDesigns;
	public List<ScheduledTask> allScheduledActions;

	public long ticks = 0; // 2^64 seconds is enough time for the rest of the universe
    internal int timescale = 1;

    public static void Update(){
		instance.UpdateInstance();
	}

	public void UpdateInstance(){
		if(ticks % 10 == 0){
			SaveWorld();
		}

		ticks += 1;

		// Use a while loop to efficiently remove tasks that are ready
		while (allScheduledActions.Count > 0 && allScheduledActions[0].completedOnTick <= ticks)
		{
			var st = allScheduledActions[0];
			st.InvokeAction();
			allScheduledActions.RemoveAt(0); // Remove the first task from the list
		}

	}

	public World(){
		worldCreationDate = DateTime.Now;
		allPlayers = new List<Player>();
		allSites = new List<ExploredSite>();
		allShipDesigns = new List<ShipDesign>();
		allShipDesigns.Add( ShipDesign.BasicExplorer() );
		allScheduledActions = new List<ScheduledTask>(); 
		timescale = 1;

	}

	#region  Save and load

	public const string FILENAME = "../world.json";

	public static void CreateOrLoad(){
		Console.WriteLine("Create or load world...");
		if(File.Exists(FILENAME)){
			instance = JSONUtilities.Deserialize<World>(FILENAME);
		}else{
			Console.WriteLine("No world found, writing one.");
			instance = new World();
			SaveWorld();
		}
	}

	public static void SaveWorld(){
		string s = JSONUtilities.SerializeToString(instance);
		
		// serialize JSON directly to a file using stream
		var serializer = JSONUtilities.serializer;
		using (StreamWriter file = File.CreateText( FILENAME + "tmp"))
		{
			serializer.Serialize(file, instance);
		}

		// This ensures interrupted file writes don't destroy data. (power loss before windows NTFS commits a write to disk)
		// the previous working backup is retained.
		if(File.Exists(FILENAME + "tmp")){
			if(File.Exists(FILENAME)){
				// retain previous version so users can manually restore broken saves.
				if(File.Exists(FILENAME +"bk")){
					File.Delete(FILENAME + "bk");
				}
				File.Move(FILENAME, FILENAME + "bk");
			}
			//atomically replace previous:
			File.Move(FILENAME +"tmp", FILENAME);
		}

	}

    internal ShipDesign GetShipDesign(string name)
    {
        return allShipDesigns.Find(x => x.name == name)!;
    }

    internal void Schedule(ScheduledTask st)
    {
        allScheduledActions.Add(st);
		// todo could sort less. could make a heap.
		allScheduledActions.OrderBy((task) => task.completedOnTick);
    }

    internal Player? FindPlayer(string playerUUID)
    {
        return allPlayers.Find(x => x.uuid == playerUUID);
    }

    internal Ship? FindShip(Player p, string? shipUUID)
    {
        return p.ships.Find(x => x.uuid == shipUUID);
    }

    #endregion

}