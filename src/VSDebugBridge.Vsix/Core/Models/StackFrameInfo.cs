namespace VSDebugBridge.Vsix.Core.Models
{
    public sealed class StackFrameInfo
    {
        public int Index { get; set; }
        public string? FunctionName { get; set; }
        public string? FileName { get; set; }
        public int LineNumber { get; set; }
    }
}
