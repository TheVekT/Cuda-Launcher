namespace Launcher.Core.Identity.Exceptions;

public class MinecraftNotPurchasedException : Exception
{
    public MinecraftNotPurchasedException(string message = "This Microsoft account does not own Minecraft: Java Edition.", Exception? innerException = null) 
        : base(message, innerException)
    {
    }
}
