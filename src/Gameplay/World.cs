
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public enum MatType
{
	Production, RetailGoods, Services, Mining, Agricultural, Aquaculture, Salvage
}

public class Material
{
	public string uuid;
	public string name;
	public MatType type;
	public string inventedByPlayerUUID;
	public DateTime inventedDate;
	public float rarity;

	public Material()
	{
		// newtonsoft
	}

	public static Material Create(string name, float rarity, MatType type, string inventedByPlayerUUID = "")
	{
		Material m = new Material();
		m.uuid = System.Guid.NewGuid().ToString();
		m.name = name;
		m.rarity = rarity;
		m.type = type;
		m.inventedByPlayerUUID = inventedByPlayerUUID;
		m.inventedDate = DateTime.Now;
		return m;
	}
}

public class World
{
	// Everything you want to save or serialize is stored on the world.
	public static World instance;

	public DateTime worldCreationDate;
	public List<Player> allPlayers;

	public List<ExploredSite> allSites;
	public List<ShipDesign> allShipDesigns;
	public List<ScheduledTask> allScheduledActions;
	public List<Material> allMats;
    public List<Offer> offers = new List<Offer>();  // List of active sell offers
    public List<Request> requests = new List<Request>(); // List of active buy requests


	public long ticks = 0; // 2^64 seconds is enough time for the rest of the universe
	public int timescale = 1;

	private GameUpdateService service;


	public static void Update(GameUpdateService service)
	{
		instance.service = service;
		instance.UpdateInstance();
	}

	public GameUpdateService GetService()
	{
		return service;
	}

	public void UpdateInstance()
	{
		if (ticks % 10 == 0)
		{
			SaveWorld();
		}

		ticks += 1;

		// Use a while loop to efficiently remove tasks that are ready
		while (allScheduledActions.Count > 0 && allScheduledActions[0].completedOnTick <= ticks)
		{
			var st = allScheduledActions[0];
			st.InvokeAction(service);
			allScheduledActions.RemoveAt(0); // Remove the first task from the list
		}

	}

	public World()
	{
		worldCreationDate = DateTime.Now;
		allPlayers = new List<Player>();
		allSites = new List<ExploredSite>();
		allShipDesigns = new List<ShipDesign>();
		allShipDesigns.Add(ShipDesign.BasicExplorer());
		allScheduledActions = new List<ScheduledTask>();
		offers = new List<Offer>();
		requests = new List<Request>();
		timescale = 30;

	}

	#region  Save and load

	public void Migrate()
	{
		if (allMats == null)
		{
			allMats = new List<Material>();
		}
		void AddIfNeeded(string name, float rarity, MatType type)
		{
			if (allMats.Find(m => m.name == name) == null)
			{
				allMats.Add(Material.Create(name, rarity, type));
			}
		}

		AddIfNeeded("Iron Ore", 10f, MatType.Mining);
		AddIfNeeded("Cobalt Ore", 1.8f, MatType.Mining);
		AddIfNeeded("Platinum Group Ore", 0.3f, MatType.Mining);
	}

	public const string FILENAME = "../world.json";

	public static void CreateOrLoad()
	{
		Log.Info("Create or load world...");
		if (File.Exists(FILENAME))
		{
			instance = JSONUtilities.Deserialize<World>(FILENAME);
			instance.Migrate();
			foreach (var item in instance.allPlayers)
			{
				item.Migrate();
			}
		}
		else
		{
			Log.Info("No world found, writing one.");
			instance = new World();
			SaveWorld();
		}
	}

	public static void SaveWorld()
	{
		string s = JSONUtilities.SerializeToString(instance);

		// serialize JSON directly to a file using stream
		var serializer = JSONUtilities.serializer;
		using (StreamWriter file = File.CreateText(FILENAME + "tmp"))
		{
			serializer.Serialize(file, instance);
		}

		// This ensures interrupted file writes don't destroy data. (power loss before windows NTFS commits a write to disk)
		// the previous working backup is retained.
		if (File.Exists(FILENAME + "tmp"))
		{
			if (File.Exists(FILENAME))
			{
				// retain previous version so users can manually restore broken saves.
				if (File.Exists(FILENAME + "bk"))
				{
					File.Delete(FILENAME + "bk");
				}
				File.Move(FILENAME, FILENAME + "bk");
			}
			//atomically replace previous:
			File.Move(FILENAME + "tmp", FILENAME);
		}

	}

	internal ShipDesign GetShipDesign(string name)
	{
		return allShipDesigns.Find(x => x.name == name)!;
	}

	internal void Schedule(ScheduledTask st)
	{
		allScheduledActions.Add(st);
		// todo could sort less often. could make a heap.
		allScheduledActions = allScheduledActions.OrderBy((task) => task.completedOnTick).ToList();
	}

	internal Player? GetPlayer(string playerUUID)
	{
		return allPlayers.Find(x => x.uuid == playerUUID);
	}


	internal Material? GetMaterial(string matUUID)
	{
		return allMats.Find(x => x.uuid == matUUID);
	}

	internal Player? GetPlayerByName(string r)
	{
		return allPlayers.Find(x => x.name.ToLowerInvariant() == r.ToLowerInvariant());
	}


	internal Ship? GetShip(Player p, string? shipUUID)
	{
		return p.ships.Find(x => x.uuid == shipUUID);
	}

	internal ExploredSite? GetSite(string uuid)
	{
		return allSites.Find(x => x.uuid == uuid);
	}

	internal ScheduledTask FindScheduledTask(string associatedScheduledTaskUUID)
	{
		return allScheduledActions.Find(x => x.uuid == associatedScheduledTaskUUID)!;
	}

	public Material? FindMat(string uuid){
        return allMats.Find(m => m.uuid == uuid);
    }



	#endregion


    /// <summary>
    /// Posts an offer to sell a material.
    /// The material is removed from the seller's inventory immediately.
    /// </summary>
    public bool PostOffer(Player seller, Material material, int amount, int pricePerUnit)
    {
        if (!seller.HasMaterial(material, amount))
        {
            seller.Send("You do not have enough of this material.");
            return false;
        }

        seller.RemoveMaterial(material, amount);
        Offer newOffer = new Offer(seller, material, amount, pricePerUnit);
        offers.Add(newOffer);

        seller.Send($"Offer posted: {amount} {material.name} for {pricePerUnit} each.");
        return true;
    }

    /// <summary>
    /// Posts a request to buy a material.
    /// The requested amount and total price are reserved from the buyer’s cash.
    /// </summary>
    public bool PostRequest(Player buyer, Material material, int amount, int pricePerUnit)
    {
        int totalCost = amount * pricePerUnit;
        if (buyer.cash < totalCost)
        {
            buyer.Send("You do not have enough cash to make this request.");
            return false;
        }

        buyer.cash -= totalCost; // Reserve funds for this request
        Request newRequest = new Request(buyer, material, amount, pricePerUnit);
        requests.Add(newRequest);

        buyer.Send($"Request posted: {amount} {material.name} for {pricePerUnit} each.");
        return true;
    }

    /// <summary>
    /// Attempts to fulfill existing requests with new offers and vice versa.
    /// </summary>
    public void ProcessTrades()
    {
        List<Offer> fulfilledOffers = new List<Offer>();
        List<Request> fulfilledRequests = new List<Request>();

        foreach (var request in requests)
        {
            var matchingOffers = offers
                .Where(o => o.Material.uuid == request.Material.uuid && o.PricePerUnit <= request.PricePerUnit)
                .OrderBy(o => o.PricePerUnit) // Prioritize cheapest offers
                .ToList();

            foreach (var offer in matchingOffers)
            {
                int tradeAmount = Math.Min(offer.Amount, request.Amount);
                int totalCost = tradeAmount * offer.PricePerUnit;

                request.Buyer.AddItem(request.Material, tradeAmount);
                offer.Seller.cash += totalCost;

                offer.Amount -= tradeAmount;
                request.Amount -= tradeAmount;

                if (offer.Amount == 0) fulfilledOffers.Add(offer);
                if (request.Amount == 0) fulfilledRequests.Add(request);

                if (request.Amount == 0) break;
            }
        }

        // Remove fulfilled orders
        offers.RemoveAll(o => fulfilledOffers.Contains(o));
        requests.RemoveAll(r => fulfilledRequests.Contains(r));
    }
}

public class Offer
{
    public Player Seller { get; }
    public Material Material { get; }
    public int Amount { get; set; }
    public int PricePerUnit { get; }

    public Offer(Player seller, Material material, int amount, int pricePerUnit)
    {
        Seller = seller;
        Material = material;
        Amount = amount;
        PricePerUnit = pricePerUnit;
    }
}

public class Request
{
    public Player Buyer { get; }
    public Material Material { get; }
    public int Amount { get; set; }
    public int PricePerUnit { get; }

    public Request(Player buyer, Material material, int amount, int pricePerUnit)
    {
        Buyer = buyer;
        Material = material;
        Amount = amount;
        PricePerUnit = pricePerUnit;
    }
}


public class OfferJsonConverter : JsonConverter<Offer>
{
    public override void WriteJson(JsonWriter writer, Offer value, JsonSerializer serializer)
    {
        JObject obj = new JObject
        {
            { "sellerUUID", value.Seller.uuid },
            { "materialUUID", value.Material.uuid },
            { "amount", value.Amount },
            { "pricePerUnit", value.PricePerUnit }
        };
        obj.WriteTo(writer);
    }

    public override Offer ReadJson(JsonReader reader, Type objectType, Offer existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        string sellerUUID = obj["sellerUUID"].ToString();
        string materialUUID = obj["materialUUID"].ToString();
        int amount = obj["amount"]?.ToObject<int>() ?? 0;
        int pricePerUnit = obj["pricePerUnit"]?.ToObject<int>() ?? 0;

        Player? seller = World.instance.GetPlayer(sellerUUID);
        Material? material = World.instance.GetMaterial(materialUUID);

        if (seller == null || material == null)
        {
            throw new JsonSerializationException("Failed to deserialize Offer: Invalid UUID reference.");
        }

        return new Offer(seller, material, amount, pricePerUnit);
    }
}

public class RequestJsonConverter : JsonConverter<Request>
{
    public override void WriteJson(JsonWriter writer, Request value, JsonSerializer serializer)
    {
        JObject obj = new JObject
        {
            { "buyerUUID", value.Buyer.uuid },
            { "materialUUID", value.Material.uuid },
            { "amount", value.Amount },
            { "pricePerUnit", value.PricePerUnit }
        };
        obj.WriteTo(writer);
    }

    public override Request ReadJson(JsonReader reader, Type objectType, Request existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        string buyerUUID = obj["buyerUUID"].ToString();
        string materialUUID = obj["materialUUID"].ToString();
        int amount = obj["amount"]?.ToObject<int>() ?? 0;
        int pricePerUnit = obj["pricePerUnit"]?.ToObject<int>() ?? 0;
		
        Player? buyer = World.instance.GetPlayer(buyerUUID);
        Material? material = World.instance.GetMaterial(materialUUID);

        if (buyer == null || material == null)
        {
            throw new JsonSerializationException("Failed to deserialize Request: Invalid UUID reference.");
        }

        return new Request(buyer, material, amount, pricePerUnit);
    }
}
