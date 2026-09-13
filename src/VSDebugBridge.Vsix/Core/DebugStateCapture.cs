using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using VSDebugBridge.Vsix.Core.Models;

namespace VSDebugBridge.Vsix.Core
{
    // Runs on the VS main thread (STA COM). Caller must have switched to the main thread.
    // No ConfigureAwait(false) anywhere: everything here touches COM objects.
    internal static class DebugStateCapture
    {
        private const int MaxDepth = 3;
        private const int ContextLines = 15;
        private const int MaxStringLength = 200;

        // Debugger-injected pseudo-members and compiler-generated noise: never useful.
        private static readonly string[] IgnoredChildren = { "Raw View", "Static members", "Non-Public members", "EqualityContract" };

        public static DebugSnapshot Capture(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var currentFile = TryGet(() => dte.ActiveDocument?.FullName);
            var currentLine = TryGet(() => (dte.ActiveDocument?.Selection as TextSelection)?.CurrentLine ?? 0);

            return new DebugSnapshot
            {
                CapturedAt = DateTime.Now,
                CurrentFile = currentFile,
                CurrentLine = currentLine,
                CurrentFunction = TryGet(() => dte.Debugger.CurrentStackFrame?.FunctionName),
                LocalVariables = CaptureLocals(dte),
                CallStack = CaptureCallStack(dte, currentFile, currentLine),
                Exception = CaptureException(dte),
                SourceContext = CaptureSourceContext(currentFile, currentLine)
            };
        }

        // COM objects throw easily (e.g. children of system types); every access is guarded.
        // Spec: long strings are truncated to 200 chars.
        private static string? Trunc(string? s)
        {
            if (s == null || s.Length <= MaxStringLength) return s;
            return s.Substring(0, MaxStringLength) + "...";
        }

        private static T TryGet<T>(Func<T> getter)
        {
            try { return getter(); }
            catch { return default; }
        }

        private static List<VariableInfo> CaptureLocals(DTE2 dte)
        {
            var result = new List<VariableInfo>();
            var locals = TryGet(() => dte.Debugger.CurrentStackFrame?.Locals);
            if (locals == null) return result;

            foreach (Expression expression in locals)
            {
                var info = ToVariableInfo(expression, depth: 1);
                if (info != null) result.Add(info);
            }
            return result;
        }

        private static VariableInfo? ToVariableInfo(Expression expression, int depth)
        {
            if (expression == null) return null;

            var info = new VariableInfo
            {
                Name = TryGet(() => expression.Name),
                Value = Trunc(TryGet(() => expression.Value)),
                Type = TryGet(() => expression.Type),
                Children = new List<VariableInfo>()
            };
            info.IsNull = info.Value == "null" || info.Value == "Nothing";

            if (depth < MaxDepth)
            {
                // EnvDTE exposes child members via DataMembers (not "Children").
                var children = TryGet(() => expression.DataMembers);
                if (children != null)
                {
                    foreach (Expression child in children)
                    {
                        var childName = TryGet(() => child.Name);
                        if (childName != null && IgnoredChildren.Contains(childName)) continue;
                        var childInfo = ToVariableInfo(child, depth + 1);
                        if (childInfo != null) info.Children.Add(childInfo);
                    }
                }
            }
            return info;
        }

        private static List<StackFrameInfo> CaptureCallStack(DTE2 dte, string? currentFile, int currentLine)
        {
            var result = new List<StackFrameInfo>();
            var frames = TryGet(() => dte.Debugger.CurrentThread?.StackFrames);
            if (frames == null) return result;

            var index = 0;
            foreach (StackFrame frame in frames)
            {
                var info = new StackFrameInfo
                {
                    Index = index++,
                    FunctionName = TryGet(() => frame.FunctionName)
                };

                // EnvDTE.StackFrame exposes no file/line info; use the active
                // document location for frame 0 (where execution is stopped).
                if (info.Index == 0 && currentFile != null)
                {
                    info.FileName = currentFile;
                    info.LineNumber = currentLine;
                }
                result.Add(info);
            }
            return result;
        }

        private static ExceptionInfo? CaptureException(DTE2 dte)
        {
            var expression = TryGet(() => dte.Debugger.GetExpression("$exception"));
            if (expression == null || !TryGet(() => expression.IsValidValue)) return null;

            var info = new ExceptionInfo
            {
                Type = TryGet(() => expression.Type),
                Message = Trunc(TryGet(() => expression.Value))
            };

            var children = TryGet(() => expression.DataMembers);
            if (children != null)
            {
                foreach (Expression child in children)
                {
                    if (string.Equals(TryGet(() => child.Name), "StackTrace", StringComparison.OrdinalIgnoreCase))
                    {
                        info.StackTrace = Trunc(TryGet(() => child.Value));
                        break;
                    }
                }
            }
            return info;
        }

                        private static string? CaptureSourceContext(string? currentFile, int currentLine)
        {
            if (currentLine <= 0 || string.IsNullOrEmpty(currentFile)) return null;

            // The VS 2026 Roslyn editor does not reliably expose the legacy EnvDTE
            // TextDocument object, so we read the file from disk instead.
            // ponytail: unsaved edits are not reflected; revisit if that bites.
            return TryGet(() =>
            {
                var all = System.IO.File.ReadAllLines(currentFile);
                var sb = new System.Text.StringBuilder();
                for (var line = Math.Max(1, currentLine - ContextLines); line <= Math.Min(currentLine + ContextLines, all.Length); line++)
                    sb.Append($"{line,6}: {all[line - 1]}").AppendLine();
                return sb.Length > 0 ? sb.ToString() : null;
            });
        }
    }
}
