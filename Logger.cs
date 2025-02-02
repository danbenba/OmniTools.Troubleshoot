/// <summary>
/// Classe utilitaire pour afficher des messages en console avec des couleurs et un préfixe [+]
/// </summary>
public static class Logger
{
    public static void StartupMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
        Console.WriteLine("IObit Driver Booster Pro 12 (Pre-activated)");
        Console.WriteLine(" ");
        Console.WriteLine("Core Version: Omni0.3");
        Console.WriteLine("Created by danbenba");
        Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
        Console.WriteLine("\n");
        Console.ResetColor();
    }

    public static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[+] " + message);
        Console.ResetColor();
    }

    public static void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("[-] " + message);
        Console.ResetColor();
    }

    public static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[x] " + message);
         Console.ResetColor();
    }
}