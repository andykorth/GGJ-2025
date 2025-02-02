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
	public List<Item> items;

	public string? currentResearch;
	public float currentResearchProgress;

	[NonSerialized]
	internal ISingleClientProxy client;
	[NonSerialized]
	internal string? connectionID;

	[NonSerialized]
	public Context currentContext;
	[NonSerialized]
    public object ContextContext;

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

    /// <summary>
    /// Adds an item to the inventory. If an item with the same material already exists, merge it by adding amounts.
    /// </summary>
    public void AddItem(Material material, int amount)
    {
        if (amount <= 0) return; // Don't add zero or negative amounts

        Item? existingItem = items.FirstOrDefault(i => i.Material.uuid == material.uuid);
        if (existingItem != null)
        {
            existingItem.Amount += amount;
        }
        else
        {
            items.Add(new Item(material, amount));
        }
    }

    /// <summary>
    /// Checks if the player has at least the given amount of a material in inventory.
    /// </summary>
    public bool HasMaterial(Material material, int requiredAmount)
    {
        Item? item = items.FirstOrDefault(i => i.Material.uuid == material.uuid);
        return item != null && item.Amount >= requiredAmount;
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

	public void SetContext<T>() where T : Context{
		Context c = InvokeCommand.allContexts[typeof(T).FullName];
        this.currentContext = c;
		SendCommandList(c);

		Log.Info($"[{name}] swap to context: {currentContext.Name})");

		c.EnterContext(this, World.instance.GetService());
    }

    private void SendCommandList(Context c)
    {
		// wow this trainwreck should be cached.
		List<string> commands = new();
		foreach(var x in c.Commands.Keys){
			if(!c.HelpAttrs[x].normallyHidden){
				commands.Add(x);
			}
		}

		var service = World.instance.GetService();
        service.SendCommandList(this, commands.ToArray(), c.Name );
    }

    internal void SetContextTo(Context c)
    {
		this.currentContext = c;
		SendCommandList(c);
		Log.Info($"[{name}] set to context: {currentContext.Name})");

		var service = World.instance.GetService();
		c.EnterContext(this, service);
    }

	
    internal void SetCaptivePrompt(string message, Func<string, bool> promptFunc)
    {
        CaptiveContext c = new()
        {
            captivePrompt = promptFunc,
            captivePromptMsg = message,
            previousContext = currentContext
        };

        SetContextTo(c);
    }

    internal void SetCaptiveYNPrompt(string message, Action<bool> responseFunc)
    {
        CaptiveContext c = new()
        {
			captivePromptMsg = message,
            previousContext = currentContext,
            captivePrompt = (string r) =>
            {
                if (r == "y" || r == "n")
                {
                    responseFunc(r == "y");
                    return true;
                }
                else
                {
                    return false;
                }
            }
        };

        SetContextTo(c);
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