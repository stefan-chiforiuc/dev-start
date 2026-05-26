namespace DevStart;

/// <summary>
/// Thrown for problems the user can fix — bad inputs, missing project state,
/// unknown capabilities, conflicting choices. Caught by the top-level handler
/// in <c>Program.cs</c> and rendered as a friendly red error + optional hint,
/// without a stack trace (unless <c>DEV_START_DEBUG=1</c>).
/// </summary>
public sealed class DevStartUserException : Exception
{
    public string? Hint { get; }

    public DevStartUserException(string message, string? hint = null) : base(message)
    {
        Hint = hint;
    }
}
