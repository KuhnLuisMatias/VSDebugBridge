namespace VSDebugBridge.Vsix.Core.Models
{
    public sealed class ExceptionInfo
    {
        public string? Type { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
    }
}
