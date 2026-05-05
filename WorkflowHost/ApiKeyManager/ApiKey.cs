namespace WorkflowHost.ApiKeyManager;

public class ApiKey
{
    public string Key { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime? Expires { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; } = true;
}
