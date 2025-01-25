## Astro Tycoon

A text oriented economic expansion game or the Global Game Jam 2025.

### Quick Setup:

Install the latest .net. I used version 9.0. You should do the same.

```
git checkout [this repo]
dotnet watch run
```

The 'watch' option will hotreload it as you make changes.

Then open localhost:3000 in your browser.

### More Setup instructions:

This tutorial was used to create the template. It might be helpful in your setup.

https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr?view=aspnetcore-9.0&tabs=visual-studio

### Features

* A robust logging engine. You can `Log.Info()` or `Log.Error()`. You don't even need a set of factories to log to the console!
* Serialization of the world.
* Hot reloading actually works reliably. 

### Gameplay

* "Exploration" as a mechanic to get a planet site.
  * Might also find the item to start a research collab.
* Construction of a production building on the planet site.
* Ship construction at different levels for better exploration ships.
* Exchange Station is where bids and asks are placed.
* Research limits the number of buildings you can construct.
    * Organizational research limits building count.

* Logistics of moving items.
* Inventory of goods implemented.
* Retail sale of goods, slow and low price, but guarenteed sales.


### Cooperative elements:
* Research Cooperative. Send a proposal to someone and they accept it. 
* User to User trade proposals.
* Invite a user to build on a planet you have a base on. 

###
Materials:

Metallic Ores
