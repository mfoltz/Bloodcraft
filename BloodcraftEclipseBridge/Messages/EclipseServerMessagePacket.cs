namespace BloodcraftEclipseBridge.Messages;

internal sealed class EclipseServerMessagePacket
{
    public string Message { get; set; } = string.Empty;

    public EclipseServerMessagePacket()
    {
    }

    public EclipseServerMessagePacket(string message)
    {
        Message = message;
    }
}
