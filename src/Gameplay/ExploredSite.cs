

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

	public string ColoredName(int rightPad = 0)
    {
        return Ascii.WrapColor( this.name.PadRight(rightPad), this.SiteColor());
    }

	public string ClassString(){
		return GoldilocksClass() ? "Goldilocks Class" : (StandardClass() ? "Habitable" : "Uninhabitable");
	}

	public string SiteColor(){
		
		if(population > 150){
			return "RebeccaPurple";
		}
		if(population > 100){
			return "chartreuse";
		}
		if(population > 60){
			return "yellow";
		}
		if(population > 30){
			return "orchid";
		}
		if(population > 12){
			return "cyan";
		}
		if(population > 0){
			return "white";
		}
		return "grey";
		
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

    public List<(Material mat, float freq)> GetFarmCrops()
    {
		List<(Material, float)> matFreq = new List<(Material, float)>();
        Random r = new Random((int) (1000* planetClass));
		foreach(Material m in World.instance.allMats){
			if(m.type == MatType.Agricultural){
				float frequency = m.rarity * 0.6f + 0.8f * m.rarity * (float) r.NextDouble();

				if(this.GoldilocksClass()){
					if(m == World.nutrigrain || m == World.veggies)
						matFreq.Add((m, frequency));
				} else if(this.StandardClass()){
					if(r.NextDouble() > 0.7f)
						matFreq.Add((m, frequency));
				}else{
					if(m == World.regolith || m == World.vacuumLichen)
						matFreq.Add((m, frequency));
				}
					
			}
		}
		return matFreq;
    }

}
