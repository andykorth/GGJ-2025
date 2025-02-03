

public interface IShortLine {
    string ShortLine(Player p, int index);
}

public class ExploredSite : IShortLine {
	public string uuid;
	public string name;
	public string discoveredByPlayerUUID;
	public DateTime discoveredDate;
	public float planetClass;
	public int population = 0;
	public int pendingPopulation = 0;

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

	public string SiteColor(){
		if(population <= 0){
			return "grey";
		}
		if(population > 0){
			return "white";
		}
		if(population > 12){
			return "cyan";
		}
		if(population > 30){
			return "orchid";
		}
		if(population > 60){
			return "yellow";
		}
		if(population > 100){
			return "chartreuse";
		}
		if(population > 150){
			return "RebeccaPurple";
		}
		return "red";
	}

    string IShortLine.ShortLine(Player p, int index)
    {
		string developmentIcon = "<[grey]-[/grey]>";
		if(population > 0){
			developmentIcon = "<[white]=[/white]>";
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
		int buildingCount = this.GetBuildingsOnSite(p).Count();
		string showIndex = index < 0 ? "" : index + ")";
		return $"   {showIndex} {developmentIcon} {name,-15} - {classMessage,-18} {buildingCount} buildings. \n";
    }

    internal string LongLine(Player player)
    {
		int playerCount = 0;
		foreach(Player p in World.instance.allPlayers){
			if(p.exploredSiteUUIDs.Contains(this.uuid)){
				playerCount += 1;
			}
		}
		string s = $"   Population: {population}k\n   Players: {playerCount}\n   Discovered by {World.instance.GetPlayer(discoveredByPlayerUUID)!.name} on {discoveredDate.ToString()}\n";
		int count = 0;
		foreach(Building b in GetBuildingsOnSite(player)){
			s += b.ShortLine(player, count);
			count += 1;
		}

		return s;
    }

    public IEnumerable<Building> GetBuildingsOnSite(Player p)
    {
		return p.buildings.Where((b) => b.siteUUID == this.uuid);
    }

    internal int GetBuildingSlots()
    {
        return 3;
    }

    public List<(Material mat, float freq)> GetOres()
    {
		List<(Material, float)> matFreq = new List<(Material, float)>();
        Random r = new Random((int) (1000* planetClass));
		foreach(Material m in World.instance.allMats){
			if(m.type == MatType.Mining){
				if(r.NextDouble() > 0.3f){
					float frequency = m.rarity * 0.6f + 0.8f * m.rarity * (float) r.NextDouble();
					matFreq.Add((m, frequency));
				}
			}
		}
		return matFreq;
    }
}

public enum BuildingType{
	Retail, Mine, Factory
}

public class Building : IShortLine {

	public string uuid;
	public string ownerPlayerUUID;
	public string siteUUID;
	public DateTime buildDate;
	public BuildingType buildingType;
	public int level;
	public string name;
    public string? associatedScheduledTaskUUID;
	public string leftoverMaterialUUID;
    public float leftovers;
    public bool isHalted;


    public string GetName(){
		return name ?? buildingType.ToString();
	}

    internal void Init(ExploredSite site, Player owner, BuildingType buildingType)
    {
        this.uuid = System.Guid.NewGuid().ToString();
		this.ownerPlayerUUID = owner.uuid;
		this.siteUUID = site.uuid;
		this.buildDate = DateTime.Now;
		this.buildingType = buildingType;
		this.level = 0;
    }

	public string GetColorTagName(int level)
	{
		return level switch
		{
			1 => "white",
			2 => "cyan",
			3 => "orchid",
			4 => "yellow",
			5 => "chartreuse",
			6 => "RebeccaPurple",
			_ => "grey" // Default case
		};
	}

    public string ShortLine(Player p, int index)
    {
		string developmentIcon = "<[grey]-[/grey]>";

		switch (level)
		{
			case 1:
				developmentIcon = "<[white]=[/white]>";
				break;
			case 2:
				developmentIcon = "<[cyan]*[/cyan]>";
				break;
			case 3:
				developmentIcon = "<[orchid]x[/orchid]>";
				break;
			case 4:
				developmentIcon = "<[yellow]#[/yellow]>";
				break;
			case 5:
				developmentIcon = "<[chartreuse]@[/chartreuse]>";
				break;
			case 6:
				developmentIcon = "<[RebeccaPurple]&[/RebeccaPurple]>";
				break;
			default:
				break;
		}
		var site = World.instance.GetSite(this.siteUUID);
		string productionMessage = GetProdMessage();
		string showIndex = index < 0 ? "" : index + ")";
		return $"   {showIndex} {developmentIcon} {GetName(),-20} {Ascii.WrapColor(site!.name, site!.SiteColor()), -20} {productionMessage}\n";
    }

	public string GetProdMessage(){
		if(associatedScheduledTaskUUID != null){
			// look up task so we know if it's done or not
			var task = World.instance.FindScheduledTask(associatedScheduledTaskUUID);
			if(task == null || task.TicksRemaining() < -5){
				Log.Error("Found a production orphan: " + associatedScheduledTaskUUID);
				this.associatedScheduledTaskUUID = null;
				return isHalted ? "Halted" :  "Idle";
			}else{
				return $"Working ({task.TicksRemaining()} s)";
			}
		}else{
			return isHalted ? "Halted" :  "Idle";
		}

	}

	internal string LongLine()
	{
		var site = World.instance.GetSite(this.siteUUID);
		string siteName = Ascii.WrapColor(site!.name, site!.SiteColor());
		string productionMessage = GetProdMessage();

		string s = "";
		s += Ascii.Header(this.GetName(), 40, GetColorTagName(this.level));
		s += $"Building Type: {this.buildingType,-20} Upgrade Level: {this.level}\n";
		s += $"Site:          {siteName,-20} {productionMessage}\n";

		if (buildingType == BuildingType.Mine)
		{
			s += "\nAvailable Ores:\n";
			int count = 0;
			foreach (var pair in site.GetOres())
			{
				s += $"   {count}) {pair.mat.name,-20} Richness: {pair.freq:0.00}\n";
				count += 1;
			}
		}
		else if (buildingType == BuildingType.Factory)
		{
			s += "\nProduction Options:\n";
			var player = World.instance.GetPlayer(this.ownerPlayerUUID)!;

			var productionMaterials = World.instance.allMats
				.Where(m => m.type == MatType.Production);

			int count = 0;
			foreach (var material in productionMaterials)
			{
				s += $"   {count})  {material.name} (Produces: {material.produced})\n";
				if (material.prereqs.Count == 0) {
					s += "     No prerequisites required.\n";
					continue;
				}

				s += "     Requires:\n";
				foreach (var (materialUUID, requiredQuantity) in material.prereqs)
				{
					Material prereqMaterial = World.instance.FindMat(materialUUID)!;
					int playerQuantity = player.GetMaterialQuantity(prereqMaterial);

					s += $"   - {prereqMaterial.name}: {playerQuantity}/{requiredQuantity}\n";
				}
				count += 1;
			}
		}

		return s;
	}

    internal void StartProd(Player p, GameUpdateService game, Building b, int index)
    {
		var site = World.instance.GetSite(this.siteUUID);
		string siteName = Ascii.WrapColor(site!.name, site!.SiteColor());
		if(buildingType == BuildingType.Mine){

			var ores = site.GetOres();
			if (index < 0 || index >= ores.Count) {
				game.Send(p, "[red]Invalid selection![/red]");
				return;
			}

			(var mat, float freq) =  ores[index];
			string s = $"Start to mine {mat.name} in {b.GetName()} on {siteName}\n";

            Schedule(p, game, b, mat, s);			
		}
		else if (buildingType == BuildingType.Factory)
        {
            // Get list of production materials
            var productionMaterials = World.instance.allMats
                .Where(m => m.type == MatType.Production).ToList();

            if (index < 0 || index >= productionMaterials.Count)
            {
                game.Send(p, "[red]Invalid selection![/red]");
                return;
            }

            Material selectedMaterial = productionMaterials[index];

            // Check if the player has enough of each prerequisite
            foreach (var (materialUUID, requiredQuantity) in selectedMaterial.prereqs)
            {
                Material prereqMaterial = World.instance.FindMat(materialUUID)!;
                int playerQuantity = p.GetMaterialQuantity(prereqMaterial);

                if (playerQuantity < requiredQuantity)
                {
                    string s = $"Production cannot start: Not enough {prereqMaterial.name}!\n";
                    game.Send(p, Ascii.Box(s, "red"));
                    return;
                }
            }

            string successMessage = $"Started producing {selectedMaterial.name} in {b.GetName()} on {siteName}.\n";
            Schedule(p, game, b, selectedMaterial, successMessage);
        }

        static void Schedule(Player p, GameUpdateService game, Building b, Material mat, string s)
        {
            // Schedule the production task
            int duration = 60 * 60 / World.instance.timescale;
            ScheduledTask st = new ScheduledTask(duration, p, b, ScheduledAction.Production);
            st.materialUUID = mat.uuid;
            st.materialAmount = mat.produced;
            World.instance.Schedule(st);

            s += $"Each hour, this will produce {mat.produced:0.00} units of {mat.name}.";

            game.Send(p, Ascii.Box(s, "green"));

            b.associatedScheduledTaskUUID = st.uuid;
            b.isHalted = false; // Ensure the building is marked as active
        }

    }
}