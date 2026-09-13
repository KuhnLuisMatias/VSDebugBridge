using System.Collections.Generic;

namespace VSDebugBridge.Vsix.Core.Models
{
    public sealed class VariableInfo
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Type { get; set; }
        public bool IsNull { get; set; }
        public List<VariableInfo> Children { get; set; } = new();
    }
}
