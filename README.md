# VS Debug Bridge

Visual Studio 2026 extension (VSIX) that captures the debugger state at a breakpoint and copies it to the clipboard as JSON, ready to paste into an AI assistant.

## What it does (Fase 0)

While debugging, run **Tools > Capture Debug State** (or press **Ctrl+Shift+D**). The extension captures:

- Current file, line and function
- Local variables (recursive, max depth 3)
- Call stack
- Active exception (type, message, stack trace), if any
- Source context: 15 lines before/after the current line

and copies a JSON snapshot to the clipboard, showing the character count in the status bar.

## Build

Requires .NET Framework 4.8 targeting (restored automatically via NuGet) and the VS 2026 MSBuild:

```
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" VSDebugBridge.sln -restore -p:Configuration=Release
```

Output: `src\VSDebugBridge.Vsix\bin\Release\VSDebugBridge.Vsix.vsix`

## Install

Double-click the `.vsix` and restart Visual Studio.

## Usage

1. Set a breakpoint and hit it.
2. Press **Ctrl+Shift+D** (or Tools > Capture Debug State).
3. Paste the JSON anywhere.

## Limitations (Fase 0 scope)

- Manual trigger only (no auto-capture on breakpoint hit).
- Captures the current thread's stack and top-level exception only (no inner exceptions, no multi-thread/parallel stacks).
- Variable expansion is capped at depth 3 and skips children the debugger cannot evaluate.
- No evaluation of arbitrary expressions; only locals and pseudo-variables like `$exception`.
- Clipboard is overwritten on each capture.
