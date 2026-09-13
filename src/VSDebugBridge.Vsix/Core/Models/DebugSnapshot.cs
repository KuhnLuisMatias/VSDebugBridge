using System;
using System.Collections.Generic;

namespace VSDebugBridge.Vsix.Core.Models
{
    public sealed class DebugSnapshot
    {
        public DateTime CapturedAt { get; set; }
        public string? CurrentFile { get; set; }
        public int CurrentLine { get; set; }
        public string? CurrentFunction { get; set; }
        public List<VariableInfo> LocalVariables { get; set; } = new();
        public List<StackFrameInfo> CallStack { get; set; } = new();
        public ExceptionInfo? Exception { get; set; }
        public string? SourceContext { get; set; }
    }
}
