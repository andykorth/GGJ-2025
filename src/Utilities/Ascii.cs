using System.Text;

public static class Ascii{


    public static string Box(string text)
    {
        // Split the input into lines
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Determine the maximum line length
        int maxLength = lines.Max(line => line.Length);

        // Create the box components
        string horizontalBorder = "+" + new string('-', maxLength + 2) + "+";
        string emptyLine = "|" + new string(' ', maxLength + 2) + "|";

        // Build the box
        var box = new StringBuilder();
        box.AppendLine(horizontalBorder);
        foreach (var line in lines)
        {
            // Center-align each line and add padding
            int padding = maxLength - line.Length;
            int paddingLeft = padding / 2;
            int paddingRight = padding - paddingLeft;

            box.AppendLine($"| {new string(' ', paddingLeft)}{line}{new string(' ', paddingRight)} |");
        }
        box.AppendLine(horizontalBorder);

        return box.ToString();
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

}