using System;

/// <summary>
/// Global notification for gameplay mistakes. Any system can raise feedback without
/// depending on the UI implementation.
/// </summary>
public static class MistakeEvent
{
    public static event Action<string> Raised;

    public static bool IsFeedbackActive { get; private set; }

    public static void Raise(string message)
    {
        Raised?.Invoke(message);
    }

    internal static void SetFeedbackActive(bool active)
    {
        IsFeedbackActive = active;
    }
}
