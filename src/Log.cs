using System;

public static class Log
{
    // ANSI color codes
    private const string Red = "\u001b[31m";
    private const string Reset = "\u001b[0m";
    
    public static void Info(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"       [{timestamp}] {message}");
    }

    public static void Error(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.ForegroundColor = ConsoleColor.Red; // Change console text color to red
        Console.Write($"{Red}Error:{Reset}");
        Console.ResetColor(); // Reset the color back to default
        Console.WriteLine($" [{timestamp}] {message}");
    }
}
