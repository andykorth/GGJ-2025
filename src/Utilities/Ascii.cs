using System.Text;
using System.Text.RegularExpressions;

public static class Ascii{

    public static string Header(string title, int headerWidth, string? color = null)
    {
        int padding = headerWidth - title.Length;
        int paddingLeft = padding / 2 - 1;
        int paddingRight = padding - paddingLeft - 1;

        string left = WrapColor(new string('=', paddingLeft), color);
        string right = WrapColor(new string('=', paddingRight), color);

        return $"{left} {title} {right}\n";
    }

    public static string WrapColor(string v, string? color)
    {
        if(string.IsNullOrEmpty(v)) return "";
        if(string.IsNullOrEmpty(color)){
            return v;
        }else{
            return $"[{color}]{v}[/{color}]";
        }
    }

    public static string Box(string text, string? color = null)
    {        
        // Split the input into lines
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Determine the maximum line length
        int maxLength = lines.Max(line => line.Length);

        // Create the box components
        string horizontalBorder = "+" + new string('-', maxLength + 2) + "+";

        horizontalBorder = WrapColor(horizontalBorder, color);
        string vert = WrapColor("|", color);
        
        // Build the box
        var box = new StringBuilder();
        box.AppendLine(horizontalBorder);
        foreach (var line in lines)
        {
            string uncolored = Regex.Replace(line, @"\[\w+\](.*?)\[/\w+\]", "$1");

            // Center-align each line and add padding
            int padding = maxLength - uncolored.Length;
            int paddingLeft = padding / 2;
            int paddingRight = padding - paddingLeft;


            box.AppendLine($"{vert} {new string(' ', paddingLeft)}{line}{new string(' ', paddingRight)} {vert}");
        }
        box.AppendLine(horizontalBorder);

        return box.ToString();
    }

    internal static string TimeAgo(TimeSpan duration)
    {
        if (duration.TotalMinutes < 60) {
            return $"{(int)duration.TotalMinutes} min ago";
        } else if (duration.TotalHours < 24) {
            return $"{(int)duration.TotalHours} hr ago";
        } else {
            return $"{(int)duration.TotalDays} days ago";
        }
    }

    internal static object Shorten(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || maxLength <= 0) {
            return string.Empty; // Handle edge cases
        }

        if (input.Length <= maxLength) {
            return input; // Return the full string if it's within the limit
        }
        return input.Substring(0, maxLength) + "...";
    }

    public static string ToRomanNumeral(int number)
    {
        if (number < 0 || number > 20)
        {
            return "?";
        }

        // Roman numeral mappings for 1 to 20
        string[] romanNumerals = 
        {
            "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
            "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX"
        };

        return romanNumerals[number];
    }



    public static string[] planetNames = new string[]
    {
        "Zorath Prime",
        "Erythia",
        "Nivora",
        "Quarion",
        "Xenora",
        "Valkis",
        "Drakos IV",
        "Sythara",
        "Oberon IX",
        "Lunara",
        "Threxal",
        "Kaelium",
        "Pyronis",
        "Velthar",
        "Arcanis",
        "Cryonos",
        "Solaris Prime",
        "Astralis",
        "Nemora",
        "Chronon",
        "Vorthal",
        "Ecliptus",
        "Zynthos",
        "Tyrion VII",
        "Mavara",
        "Krythos",
        "Vega Nexus",
        "Aurion",
        "Keldara",
        "Zenthos",
        "Galvona",
        "Phalora",
        "Xarthis",
        "Nythera",
        "Ravos",
        "Ignis Minor",
        "Seron",
        "Altaris",
        "Lycoris",
        "Orionis",
        "Ythalia",
        "Drathune",
        "Zephyros",
        "Eryndor",
        "Morvath"
    };
public static string[] relicNames = new string[]
{
    "Crystalline Shard of Zorath",
    "Ancient Tablet of Erithon",
    "Obsidian Idol of the Void",
    "Stellar Compass",
    "Phantom Core Fragment",
    "Eclipse Talisman",
    "Runestone of the Forgotten",
    "Orb of Temporal Flux",
    "Luminarion Prism",
    "Celestial Beacon",
    "Vortex Amulet",
    "Shard of the Eternal Flame",
    "Darklight Mirror",
    "Aeon Sigil",
    "Whispering Mask",
    "Chalice of Cosmic Essence",
    "Veil of Aetherial Winds",
    "Glyph of Resonance",
    "Key of the Abyss",
    "Crown of the Starborn"
};

public static string[] developmentProjects = new string[]
{
    "Hydroponic Farms",
    "Fusion Reactor Grid",
    "Atmospheric Stabilizers",
    "Quantum Health Centers",
    "AI-Driven Education",
    "Maglev Transport Lines",
    "Orbital Habitat Expansion",
    "Holo-Theater Network",
    "Terraforming Operations",
    "Nanotech Waste Recycling",
    "Genetic Diversity Vault",
    "Cryogenic Medical Labs",
    "Bio-Dome Construction",
    "Subterranean Cities",
    "Asteroid Mining Facility",
    "Plasma Defense Array",
    "Neural Link Uplink",
    "Weather Control Stations",
    "Interstellar Trade Nexus",
    "Dark Matter Research Lab"
};


}