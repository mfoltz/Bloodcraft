namespace BloodcraftEclipseBridge.Messages;

internal sealed class EclipseRegistrationPacket
{
    public string Message { get; set; } = string.Empty;

    public EclipseRegistrationPacket()
    {
    }

    public EclipseRegistrationPacket(string message)
    {
        Message = message;
    }
}
