


using System.Security.Cryptography.X509Certificates;

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
	public List<(string materialUUID, int quantity)> prereqs;
	public int produced;
	public float baseCost;

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
	public const string FILENAME = "../world.json";
	public const float RESEARCH_BOOST = 0.15f;


	// Everything you want to save or serialize is stored on the world.
	public static World instance;

	public DateTime worldCreationDate;
	public List<Player> allPlayers;

	public List<ExploredSite> allSites;
	public List<ShipDesign> allShipDesigns;
	public List<ScheduledTask> allScheduledActions;
	public List<Material> allMats;
	public List<ResearchProject> allResearch;

	public List<Offer> offers;
	public List<Request> requests;

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
			allScheduledActions.RemoveAt(0); // Remove the first task from the list
			st.InvokeAction(service);
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
		allResearch = new List<ResearchProject>();
		timescale = 30;

	}

	#region  Save and load

	public static Material iron, cobalt, platinum, hydrocarbons, metalFurniture, polyethelene, hullPlates, synthCotton, nutrigrain, veggies, regolith, vacuumLichen;

	public static Material workwear, luxuryFurniture, deluxeMeals, prepackagedMeals, synthFabric;

	public void Migrate()
	{
		if (allMats == null) allMats = new List<Material>();
		if (offers == null) offers = new List<Offer>();
		if (requests == null) requests = new List<Request>();
		if (allResearch == null) allResearch = new List<ResearchProject>();

		int rem = offers.RemoveAll(o => string.IsNullOrEmpty(o.materialUUID));
		if (rem > 0)
		{
			Log.Error($"Removed {rem} invalid offers from CX exchange. Missing materialUUID.");
		}
		rem = requests.RemoveAll(o => string.IsNullOrEmpty(o.materialUUID));
		if (rem > 0)
		{
			Log.Error($"Removed {rem} invalid requests from CX exchange. Missing materialUUID");
		}

		Material AddIfNeeded(string name, float rarity, MatType type)
		{
			if (allMats.Find(m => m.name == name) == null)
			{
				allMats.Add(Material.Create(name, rarity, type));
			}
			return GetMaterialByName(name);
		}

		Material AddProducedItem(string name, int produced, List<(string materialUUID, int quantity)> prereqs)
		{
			if (allMats.Find(m => m.name == name) == null)
			{
				var mat = Material.Create(name, 1.0f, MatType.Production);
				mat.prereqs = prereqs;
				mat.produced = produced;

				allMats.Add(mat);
			}
			return GetMaterialByName(name);
		}

		iron = AddIfNeeded("Iron Ore", 10f, MatType.Mining);
		cobalt = AddIfNeeded("Cobalt Ore", 1.85f, MatType.Mining);
		platinum = AddIfNeeded("Platinum Group Ore", 0.4f, MatType.Mining);
		hydrocarbons = AddIfNeeded("Hydrocarbons", 10f, MatType.Mining);

		metalFurniture = AddProducedItem("Metal Furniture", 1, [(iron.uuid, 1)]);
		metalFurniture.baseCost = 10;
		metalFurniture.type = MatType.RetailGoods;

		polyethelene = AddProducedItem("Polyethelene", 100, [(hydrocarbons.uuid, 1)]);

		hullPlates = AddProducedItem("Hull Plates", 1, [(iron.uuid, 3), (polyethelene.uuid, 20)]);

		synthCotton = AddIfNeeded("Cotton", 8f, MatType.Agricultural);
		nutrigrain = AddIfNeeded("Nutrigrain", 3f, MatType.Agricultural);
		veggies = AddIfNeeded("Veggies", 3f, MatType.Agricultural);
		regolith = AddIfNeeded("Regolith", 12f, MatType.Agricultural);
		vacuumLichen = AddIfNeeded("Vacuum Lichen", 12f, MatType.Agricultural);

		synthFabric = AddProducedItem("SynthFabric", 10, [(synthCotton.uuid, 5), (polyethelene.uuid, 2)]);

		prepackagedMeals = AddProducedItem("Pre-Packaged Meals", 20, [(nutrigrain.uuid, 5), (polyethelene.uuid, 1)]);
		prepackagedMeals.type = MatType.RetailGoods;
		prepackagedMeals.baseCost = 15;

		deluxeMeals = AddProducedItem("Deluxe Meals", 40, [(veggies.uuid, 5), (polyethelene.uuid, 1)]);
		deluxeMeals.type = MatType.RetailGoods;
		deluxeMeals.baseCost = 25;

		luxuryFurniture = AddProducedItem("Luxury Furniture", 2, [(metalFurniture.uuid, 1), (synthCotton.uuid, 2)]);
		luxuryFurniture.type = MatType.RetailGoods;
		luxuryFurniture.baseCost = 60;

		workwear = AddProducedItem("Industrial Workwear", 5, [(synthCotton.uuid, 3), (iron.uuid, 1)]);
		workwear.type = MatType.RetailGoods;
		workwear.baseCost = 30;

	}

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


	internal Player? GetPlayer(string playerUUID)
	{
		return allPlayers.Find(x => x.uuid == playerUUID);
	}


	internal Material? GetMaterial(string matUUID)
	{
		var m = allMats.Find(x => x.uuid == matUUID);
		if (m == null)
		{
			Log.Error($"Could not find material with UUID: {matUUID}");
		}
		return m;
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

	public Material? FindMat(string uuid)
	{
		return allMats.Find(m => m.uuid == uuid);
	}

	#endregion

	/// <summary>
	/// Posts an offer to sell a material.
	/// The material is removed from the seller's inventory immediately.
	/// </summary>
	public bool PostOffer(Player seller, Material material, int amount, int pricePerUnit)
	{

		// count offers:
		int c = OfferRequestCount(seller);
		if(c >= seller.GetMaxExchangeOrders()){
			seller.Send($"You already have {c} offers and requests. To make more, upgrade your [green]Commerce Bureau[/green] at your headquarters.");
			return false;
		}

		if (!seller.HasMaterial(material, amount))
		{
			seller.Send($"You do not have enough {material}. You have {seller.GetMaterialQuantity(material)}. You wanted to sell {amount}. ");
			return false;
		}

		seller.RemoveMaterial(material, amount);
		Offer newOffer = new Offer(seller, material, amount, pricePerUnit);
		offers.Add(newOffer);

		Log.Info($"[{seller.name}] Offer posted: {amount} {material.name} for {pricePerUnit} each.");
		seller.Send($"Offer posted: {amount} {material.name} for {pricePerUnit} each.");
		return true;
	}

	/// <summary>
	/// Posts a request to buy a material.
	/// The requested amount and total price are reserved from the buyer’s cash.
	/// </summary>
	public bool PostRequest(Player buyer, Material material, int amount, int pricePerUnit)
	{

		// count offers:
		int c = OfferRequestCount(buyer);
		if(c >= buyer.GetMaxExchangeOrders()){
			buyer.Send($"You already have {c} offers and requests. To make more, upgrade your [green]Commerce Bureau[/green] at your headquarters.");
			return false;
		}


		int totalCost = amount * pricePerUnit;
		if (buyer.cash < totalCost)
		{
			buyer.Send("You do not have enough cash to make this request.");
			return false;
		}

		buyer.cash -= totalCost; // Reserve funds for this request
		Request newRequest = new Request(buyer, material, amount, pricePerUnit);
		requests.Add(newRequest);

		Log.Info($"[{buyer.name}] Request posted: {amount} {material.name} for {pricePerUnit} each.");
		buyer.Send($"Request posted: {amount} {material.name} for {pricePerUnit} each.");
		return true;
	}

    public int OfferRequestCount(Player p)
    {
        return offers.Count((offer) => offer.Seller == p) + requests.Count((offer) => offer.Buyer == p);
    }

    /// <summary>
    /// Attempts to fulfill existing requests with new offers and vice versa.
    /// </summary>
    public void ProcessTrades()
	{

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

				if (request.Amount == 0) break;
			}
		}

		// Remove fulfilled orders
		offers.RemoveAll(o => o.Amount <= 0);
		requests.RemoveAll(r => r.Amount <= 0);

		offers = offers.OrderByDescending(x => x.PricePerUnit)
		.OrderBy(x => x.Material.name).ToList();

		requests = requests.OrderByDescending(x => x.PricePerUnit)
		.OrderBy(x => x.Material.name).ToList();
	}

	internal List<Offer> GetOffers()
	{
		return offers;
	}
	internal List<Request> GetRequests()
	{
		return requests;
	}

	internal Material? GetMaterialByName(string materialName)
	{
		return this.allMats.FirstOrDefault(o => o.name.ToLowerInvariant().StartsWith(materialName.ToLowerInvariant()));
	}

	internal void Schedule(ScheduledTask st)
	{
		if (allScheduledActions.RemoveAll(x => x.uuid == st.uuid) > 0)
		{
			Log.Error($"Removed duplicate scheduled task {st.uuid} while rescheduling.");
		}
		allScheduledActions.Add(st);
		// todo could sort less often. could make a heap.
		allScheduledActions = allScheduledActions.OrderBy((task) => task.completedOnTick).ToList();
	}

	internal void StopSchedule(string? associatedScheduledTaskUUID)
	{
		if (allScheduledActions.RemoveAll(x => x.uuid == associatedScheduledTaskUUID) <= 0)
		{
			Log.Error($"Attempted to remove scheduled item {associatedScheduledTaskUUID}, but it was not found.");
		}
	}

	internal List<Material> FactoryMaterials()
	{
		return this.allMats
				.Where(m => m.type == MatType.Production || m.type == MatType.RetailGoods).ToList();
	}
}

