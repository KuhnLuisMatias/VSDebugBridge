using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using VSDebugBridge.Vsix.Commands;

namespace VSDebugBridge.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("0E045F9B-EFAF-419F-BE7E-55E6994D8B94")]
    public sealed class VSDebugBridgePackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null) return;

            var commandId = new CommandID(new Guid("1FA3D4BF-D03D-41A0-9B31-05BCD41142D3"), 0x0100);
            var command = new OleMenuCommand(CaptureDebugStateCommand.Invoke, commandId);
            commandService.AddCommand(command);
        }
    }
}
