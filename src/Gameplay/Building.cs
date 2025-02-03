
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
				Log.Error("Found a running building without a scheduled task: " + associatedScheduledTaskUUID + " task null: " + (task == null));
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

            Schedule(p, game, b, mat, s, freq);			
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
            Schedule(p, game, b, selectedMaterial, successMessage, selectedMaterial.produced);
        }

        static void Schedule(Player p, GameUpdateService game, Building b, Material mat, string s, float amountProduced)
        {
            // Schedule the production task
            int duration = 60 * 60 / World.instance.timescale;
            ScheduledTask st = new ScheduledTask(duration, p, b, ScheduledAction.Production);
            st.materialUUID = mat.uuid;
            st.materialAmount = amountProduced;
            World.instance.Schedule(st);

            s += $"Each hour, this will produce {mat.produced:0.00} units of {mat.name}.";

            game.Send(p, Ascii.Box(s, "green"));

            b.associatedScheduledTaskUUID = st.uuid;
            b.isHalted = false; // Ensure the building is marked as active
        }

    }
}