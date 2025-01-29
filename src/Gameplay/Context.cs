
using System.Reflection;
using Microsoft.AspNetCore.SignalR;

public abstract class Context
{
    public abstract string Name { get; }
    public virtual bool rootContext { get { return false;}  }

    internal Dictionary<string, MethodInfo> Commands { get; } = new();
    internal Dictionary<string, GameCommandAttribute> HelpAttrs { get; } = new();
}
