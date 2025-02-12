using Newtonsoft.Json;
using System;

public class Item : IShortLine
{
    [JsonIgnore] 
    public Material Material => World.instance.GetMaterial(matUUID);
    
    public string matUUID;
    public int Amount;

    public Item(){}

    public Item(Material material, int amount)
    {
        matUUID = material.uuid;
        Amount = amount;
    }

    public string LongLine() => $"{Material.name} - {Amount} units\nRarity: {Material.rarity}\nType: {Material.type}";

    public string ShortLine(Player p, int index)
    {
        string showIndex = index < 0 ? "" : index + ")";
        string qty = $"{Amount} / {p.GetMaxStorageFor(Material)}";
        return $"   {showIndex} {Material.name,-20} {qty, -20}\n";
    }
}


public class Offer
{
    public Offer(){}

    [JsonIgnore] 
    public Player Seller => World.instance.GetPlayer(sellerUUID);

    [JsonIgnore] 
    public Material Material => World.instance.GetMaterial(materialUUID);

    public string sellerUUID;
    public string materialUUID;
    public int Amount;
    public int PricePerUnit;

    public Offer(Player seller, Material material, int amount, int pricePerUnit)
    {
        sellerUUID = seller.uuid;
        materialUUID = material.uuid;
        Amount = amount;
        PricePerUnit = pricePerUnit;
    }
}


public class Request
{

    public Request(){}

    [JsonIgnore] 
    public Player Buyer => World.instance.GetPlayer(buyerUUID);

    [JsonIgnore] 
    public Material Material => World.instance.GetMaterial(materialUUID);

    public string buyerUUID;
    public string materialUUID;
    public int Amount;
    public int PricePerUnit;

    public Request(Player buyer, Material material, int amount, int pricePerUnit)
    {
        buyerUUID = buyer.uuid;
        materialUUID = material.uuid;
        Amount = amount;
        PricePerUnit = pricePerUnit;
    }
}

