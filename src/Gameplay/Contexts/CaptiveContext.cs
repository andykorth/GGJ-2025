
public class CaptiveContext : Context {

    public override string Name => captivePromptMsg;

	internal Func<string, bool>? captivePrompt;
	internal string? captivePromptMsg;
	internal Context nextContext;

    public override void EnterContext(Player p, GameUpdateService game)
    {
        game.Send(p, captivePromptMsg);
    }

    internal override void Invoke(Player p, GameUpdateService game, string command, string args)
    {
        Log.Info($"[{p.name}] captive context invoked [{command} {args}]");
        
        game.Send(p, $"[yellow]>[/yellow][magenta]>{command} {args}[/magenta]");

        p.insideContextCallback = true;
        bool b = captivePrompt!(command);
        p.insideContextCallback = false;

        if(b){
            // done with captive prompt!
            p.SetContextTo(nextContext);
        }else{
            // repeat the prompt, they did it wrong.
            p.Send("[red]>[/red]" +captivePromptMsg);
        }
    }
}
