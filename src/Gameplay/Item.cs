using Newtonsoft.Json;
using System;

public class Item
{
    [JsonIgnore] 
    public Material Material { get;  set; }
    public int Amount { get;  set; }

    public Item(Material material, int amount)
    {
        Material = material;
        Amount = amount;
    }

    public string ShortLine() => $"{Material.name}: {Amount}";

    public string LongLine() => $"{Material.name} - {Amount} units\nRarity: {Material.rarity}\nType: {Material.type}";
}


public class ItemJsonConverter : JsonConverter<Item>
{
    public override void WriteJson(JsonWriter writer, Item value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("MaterialUUID");
        writer.WriteValue(value.Material.uuid);  // Store only the UUID
        writer.WritePropertyName("Amount");
        writer.WriteValue(value.Amount);
        writer.WriteEndObject();
    }

    public override Item ReadJson(JsonReader reader, Type objectType, Item existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        string materialUUID = null;
        int amount = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = (string)reader.Value;
                reader.Read();
                if (propertyName == "MaterialUUID") materialUUID = (string)reader.Value;
                else if (propertyName == "Amount") amount = Convert.ToInt32(reader.Value);
            }
            else if (reader.TokenType == JsonToken.EndObject)
                break;
        }

        Material material = World.instance.FindMat(materialUUID)!;
        return material != null ? new Item(material, amount) : throw new JsonSerializationException($"Material with UUID {materialUUID} not found.");
    }
}
