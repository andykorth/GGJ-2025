
public class CaptiveContext : Context {

    public override string Name => captivePromptMsg;

	internal Func<string, bool>? captivePrompt;
	internal string? captivePromptMsg;
	internal Context previousContext;

    public override void EnterContext(Player p, GameUpdateService game)
    {
        game.Send(p, captivePromptMsg);
    }

    internal override void Invoke(Player p, GameUpdateService game, string command, string args)
    {
        game.Send(p, $"[yellow]>[/yellow][magenta]>{command} {args}[/magenta]");

        bool b = captivePrompt!(command);
        if(b){
            // done with captive prompt!
            p.SetContextTo(previousContext);
        }else{
            // repeat the prompt, they did it wrong.
            p.Send(captivePromptMsg);
        }
    }
}
