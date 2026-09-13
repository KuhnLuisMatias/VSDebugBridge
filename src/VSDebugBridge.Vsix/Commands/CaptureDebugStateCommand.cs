using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VSDebugBridge.Vsix.Core;

namespace VSDebugBridge.Vsix.Commands
{
    internal static class CaptureDebugStateCommand
    {
        public static void Invoke(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
                if (dte == null) throw new InvalidOperationException("No se pudo obtener el servicio DTE.");

                CaptureAndCopy(dte);
            }
            catch (Exception ex)
            {
                LogAndShowError(ex);
            }
        }

        private static void CaptureAndCopy(DTE2 dte)
        {
            var mode = dte.Debugger.CurrentMode;
            if (mode != dbgDebugMode.dbgBreakMode)
            {
                var message = mode == dbgDebugMode.dbgDesignMode
                    ? "No hay sesión de debug activa"
                    : "El debugger no está en modo break";
                ShowInfo(message);
                return;
            }

            var snapshot = DebugStateCapture.Capture(dte);
            var json = JsonConvert.SerializeObject(snapshot, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            System.Windows.Clipboard.SetText(json);
            dte.StatusBar.Text = $"Estado del debugger copiado ({json.Length} caracteres)";
        }

        private static void ShowInfo(string message)
        {
            // Overload: (provider, message, title, helpTopic, icon, msgButton, defaultButton).
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                "VS Debug Bridge",
                null,
                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static void LogAndShowError(Exception ex)
        {
            try
            {
                var activityLog = ServiceProvider.GlobalProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
                activityLog?.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    "VSDebugBridge",
                    ex.ToString());
            }
            catch
            {
                // Logging must never mask the original error.
            }

            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                "Error capturando el estado del debugger: " + ex.Message,
                "VS Debug Bridge",
                null,
                OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
