## Global Game Jam, text game template

This is a template for making a persistent multi-user text based game that can be played through a web page, which is more accessible than requiring users to telnet into it.

### Quick Setup:

Install the latest .net. I used version 9.0. You should do the same.

`git checkout [this repo]`

`dotnet run`

or 

`dotnet watch run`

The 'watch' option will hotreload it as you make changes.

Then open localhost:3000 in your browser.

### More Setup instructions:

This tutorial was used to create the template. It might be helpful in your setup.

https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr?view=aspnetcore-9.0&tabs=visual-studio

### Features

* A robust logging engine. You can `Log.Info()` or `Log.Error()`. You don't even need a set of factories to log to the console!
* Serialization.