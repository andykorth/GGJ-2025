
public class ExploredSite {
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

    internal string ShortLine(int index = -1)
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
		string showIndex = index < 0 ? "" : index + ")";
		return $"   {showIndex} {developmentIcon} {name} - {classMessage}\n";
    }

    internal string LongLine()
    {
		int playerCount = 0;
		foreach(Player p in World.instance.allPlayers){
			if(p.exploredSiteUUIDs.Contains(this.uuid)){
				playerCount += 1;
			}
		}
		return $"   Population: {population}k\n   Players: {playerCount}\n   Discovered by {World.instance.GetPlayer(discoveredByPlayerUUID)!.name} on {discoveredDate.ToString()}\n";
    }
}
