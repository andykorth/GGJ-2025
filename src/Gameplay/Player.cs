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
	public List<int> relicIDs;
	public DateTime lastActivity;
	public DateTime created;
	public List<Message> messages;
	public List<Building> buildings;
	public List<Item> items;

    public int fleetCommandOffice;
    public int adminOffice;
    public int logisticsOffice;
    public int commerceBureauOffice;
    public int researchOffice;


	[NonSerialized]
	internal ISingleClientProxy client;
	[NonSerialized]
	internal string? connectionID;

	[NonSerialized]
	public Context currentContext;
	[NonSerialized]
    public object ContextData; // what the currentContext is pointing at

	[NonSerialized]
    internal bool insideContextCallback = false;


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
		if (items == null) items = new List<Item>();

		foreach(var ship in ships){
			ship.Migrate();
		}

		int invalidRemoved = items.RemoveAll( (item) => item.Material == null);
		if(invalidRemoved > 0) Log.Error($"Removed {invalidRemoved} invalid items from {this.name} inventory");

		items.RemoveAll( (item) => item.Amount <= 0);
		
	    fleetCommandOffice = Math.Max(1, fleetCommandOffice);
        adminOffice = Math.Max(1, adminOffice);
        logisticsOffice = Math.Max(1, logisticsOffice);
        commerceBureauOffice = Math.Max(1, commerceBureauOffice);
        researchOffice = Math.Max(1, researchOffice);
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

		Migrate();
	}


	public int GetMaxBuildings(){
		return adminOffice * 2 + 2;
	}

	public int GetMaxStorageInBaseCost(){
		return 1000 + 1000 * logisticsOffice;
	}

	public int GetMaxStorageFor(Material m){
		float c = m.baseCost * 0.25f;
		if(c <= 0){
			c = 5f / m.rarity;
		}
		return (int) (GetMaxStorageInBaseCost() / c);
	}

	public int GetMaxExchangeOrders(){
		return (int) 5 + 5 * commerceBureauOffice;
	}

    /// <summary>
    /// Adds an item to the inventory. If an item with the same material already exists, merge it by adding amounts.
    /// </summary>
	/// 
    public bool AddItem(Material material, int amount)
    {
        if (amount <= 0) return true; // Don't add zero or negative amounts

        Item? existingItem = items.FirstOrDefault(i => i.Material.uuid == material.uuid);
		if(existingItem == null){
			existingItem = new Item(material, 0);
			items.Add(existingItem);
		}
		int newAmt = Math.Max(0, existingItem.Amount) + amount;
		int maxStorage = this.GetMaxStorageFor(material);
		bool exceedsMax = newAmt > maxStorage;
		existingItem.Amount = Math.Min(maxStorage, newAmt);
		return exceedsMax;
    }

    /// <summary>
    /// Checks if the player has at least the given amount of a material in inventory.
    /// </summary>
    public bool HasMaterial(Material material, int requiredAmount)
    {
        Item? item = items.FirstOrDefault(i => i.Material.uuid == material.uuid);
        return item != null && item.Amount >= requiredAmount;
    }

    public int GetMaterialQuantity(Material material)
    {
        Item? item = items.FirstOrDefault(i => i.Material.uuid == material.uuid);
        return item != null ? item.Amount  : 0;
    }

    /// <summary>
    /// Removes a specified amount of a material from the inventory.
    /// Returns true if successful, false if there was not enough material.
    /// </summary>
    public bool RemoveMaterial(Material material, int amount)
    {
        Item? item = items.FirstOrDefault(i => i.Material.uuid == material.uuid);
        if (item == null || item.Amount < amount) return false;

        item.Amount -= amount;
        if (item.Amount == 0)
        {
            items.Remove(item); // Remove empty items from inventory
        }
        return true;
    }


    private void SendCommandList(Context c)
    {
		var (commands, commandHelps) = c.GetCommandList();

		var service = World.instance.GetService();
        service.SendCommandList(this, commands.ToArray(), commandHelps.ToArray(), c.Name );
    }

	public void SetContext<T>() where T : Context{
		Context c = InvokeCommand.allContexts[typeof(T).FullName];
		SetContextTo(c);
    }

    public void SetContextTo(Context c)
    {
		if(this.insideContextCallback){
			CaptiveContext captive = currentContext as CaptiveContext;
			if(captive != null){
				Log.Info($"[{name}] queueing a change to context: {currentContext.Name} [{currentContext.GetType()}]. We will later pop to it.");
				captive.nextContext = c;
				return;
			}else{
				Log.Error($"[{name}] Context change to [{currentContext.GetType()}] while inside a context callback! After the callback your state will get wrecked.");
			}
		}
		this.currentContext = c;
		SendCommandList(c);
		Log.Info($"[{name}] set to context: {currentContext.Name} [{currentContext.GetType()}]");

		c.EnterContext(this, World.instance.GetService());
    }

    internal void SetCaptivePrompt(string message, Func<string, bool> promptFunc, bool backToMain = false)
    {
		Context goBackToContext = backToMain ? InvokeCommand.GetContext<MainContext>() : this.currentContext;
        CaptiveContext c = new()
        {
            captivePrompt = promptFunc,
            captivePromptMsg = message,
            nextContext = goBackToContext
        };

        SetContextTo(c);
    }

    internal void SetCaptivePrompt(string message, Context prevContext, Func<string, bool> promptFunc )
    {
        CaptiveContext c = new()
        {
            captivePrompt = promptFunc,
            captivePromptMsg = message,
            nextContext = prevContext
        };
        SetContextTo(c);
    }

    internal void SetCaptiveYNPrompt(string message, Action<bool> responseFunc)
    {
        CaptiveContext c = new()
        {
			captivePromptMsg = message,
            nextContext = currentContext,
            captivePrompt = (string r) =>
            {
                if (r == "y" || r == "n") {
                    responseFunc(r == "y");
                    return true;
                } else {
                    return false;
                }
            }
        };

        SetContextTo(c);
    }


    internal void SetCaptiveSelectPlayerPrompt(string msg, Action<Player> callback)
    {
		Player p = this;
        SetCaptivePrompt( msg,
            (string response) => {
                string r = response.ToLower();
                // try to find a player name
                Player? found = World.instance.GetPlayerByName(r);
                if(r == "cancel" || r == "who" || found != null){
                    if(r == "who"){
                        MainContext.WhoCmd(this,  "");
                        return false; // keep them in the captive prompt
                    }
                    if(r == "cancel")
                        p.Send("Message canceled.");
                    if(found != null){
                        callback(found!);
                        return true; // exit captive prompt after.
                    }else{
                        p.Send( $"No one found with name `{r}`");
                    }
                    return true;
                }else{
                    return false;
                }
            });
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
		currentContext.Invoke(this, game, command, args);
    }

    internal List<ResearchProject> GetResearchProjects()
    {
		var list = World.instance.allResearch.Where(r => r.leaderPlayer == this || r.partnerPlayer == this).ToList();

		return list;
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

public class ResearchProject : IShortLine{
	
	public string uuid;
	public string? researchMaterialUUID;
	public string leaderPlayerUUID;
	public string partnerPlayerUUID;
	public DateTime researchStartTime;
	public int researchCost;
	public string relicName;

	public ResearchProject(){}

	[JsonIgnore]
	public Material researchedMaterial{
		set{
			researchMaterialUUID = value.uuid;
		}
		get {
			return World.instance.GetMaterial(researchMaterialUUID)!;
		}
	}

	[JsonIgnore]
	public Player partnerPlayer{
		set{
			partnerPlayerUUID = value.uuid;
		}
		get {
			return World.instance.GetPlayer(partnerPlayerUUID)!;
		}
	}

	[JsonIgnore]
	public Player leaderPlayer{
		set{
			leaderPlayerUUID = value.uuid;
		}
		get {
			return World.instance.GetPlayer(leaderPlayerUUID)!;
		}
	}

	public string ShortLine(Player p, int index = -1)
	{
		string showIndex = index < 0 ? "" : index + ")";
		return $"   {showIndex} [cyan]{researchedMaterial.name,-20}[/cyan] {(p == leaderPlayer? "Leader" : "Partner" ),-15} {World.RESEARCH_BOOST*100:0.00}% Boost   {researchStartTime}\n";
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
		TextMail, Invitation, ResearchInvitation,
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
		return $"  {index})    [{Ascii.TimeAgo(now - this.sent)}] {status} - [yellow]{this.type}[/yellow] from {from}: {Ascii.Shorten(this.contents, 20)}\n";

    }
}