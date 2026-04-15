using System.Text.Json;

namespace OpenDork.WrapperBridge;

public record WrapperStatus(string ActiveProvider, string ActiveModel, bool Ready, DateTimeOffset? ResetAtUtc);
public record SwitchRequest(string Reason, string FromProvider, DateTimeOffset RequestedAtUtc);

[Obsolete("Deprecated transitional component. Use opendork-cli + opendork-state directly.")]
public sealed class FileIpcBridge
{
    public string StatusFile { get; }
    public string RequestsFile { get; }
    public FileIpcBridge(string root)
    {
        Directory.CreateDirectory(root);
        StatusFile = Path.Combine(root, "wrapper-status.json");
        RequestsFile = Path.Combine(root, "switch-requests.jsonl");
    }

    public WrapperStatus ReadStatus()
    {
        if (!File.Exists(StatusFile)) return new("unknown", "unknown", false, null);
        return JsonSerializer.Deserialize<WrapperStatus>(File.ReadAllText(StatusFile), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new("unknown", "unknown", false, null);
    }

    public void RequestSwitch(SwitchRequest request)
        => File.AppendAllText(RequestsFile, JsonSerializer.Serialize(request) + Environment.NewLine);
}
