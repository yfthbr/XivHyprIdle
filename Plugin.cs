using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Task = System.Threading.Tasks.Task;

namespace XivHyprIdle;

// ReSharper disable once ClassNeverInstantiated.Global - instantiated by Dalamud
// ReSharper disable once UnusedMember.Global - instantiated by Dalamud
// ReSharper disable once PartialTypeWithSinglePart - instantiated by Dalamud
public sealed partial class Plugin : IAsyncDalamudPlugin
{
    private readonly CancellationTokenSource _cts = new();

    private Task? _tracker;

    private readonly UdpClient _udpListener;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<S>();
        if (!int.TryParse(Environment.GetEnvironmentVariable("XIVHYPRIDLE_PORT"), out var port))
            port = 15432;
        _udpListener = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
    }

    public Task LoadAsync(CancellationToken _)
    {
        // CancellationToken is for the duration of plugin loading only, avoid using it here.
        // DisposeAsync will get called anyway, which handles cancellation of the inner task.
        _tracker = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync(_cts.Token);
                    var str = Encoding.UTF8.GetString(result.Buffer);
                    var inFocus = str == "true";
                    S.Log.Info($"Focus changed to: {inFocus}, with: {str}");
                    await S.Framework.RunOnFrameworkThread(() =>
                    {
                        unsafe
                        {
                            Framework.Instance()->WindowInactive = !inFocus;
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    S.Log.Error($"Error in udp reception: {ex}");
                }
            }
        });
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _tracker!;
        _udpListener.Dispose();
        _cts.Dispose();
    }
}

public class S
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local - required for plugin interface
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local - required for plugin interface
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
}
