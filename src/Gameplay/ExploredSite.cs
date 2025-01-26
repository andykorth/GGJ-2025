

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
		int buildingCount = this.GetBuildings(p).Count();
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
		foreach(Building b in GetBuildings(player)){
			s += b.ShortLine(player, count);
			count += 1;
		}

		return s;
    }

    public IEnumerable<Building> GetBuildings(Player p)
    {
		return p.buildings.Where((b) => b.siteUUID == this.uuid);
    }

    internal int GetBuildingSlots()
    {
        return 3;
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
		string productionMessage = " - asdf";
		string showIndex = index < 0 ? "" : index + ")";
		return $"   {showIndex} {developmentIcon} {GetName(),-20} {Ascii.WrapColor(site!.name, site!.SiteColor()), -20} {productionMessage}\n";
    }

    internal string LongLine()
    {
		return "";
    }

}