namespace Launcher.Core.Game.Exceptions;

public class InvalidJvmArgumentsException : Exception
{
    public string? RejectedArgument { get; }

    public InvalidJvmArgumentsException(string message, string? rejectedArgument = null) 
        : base(message)
    {
        RejectedArgument = rejectedArgument;
    }
}
