using System.Net;
using System.Net.Sockets;

namespace SafariBooksDownload.Tests;

/// <summary>Minimal HttpListener on a free loopback port; the handler decides every response.</summary>
internal sealed class LoopbackServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Task _loop;

    public string BaseUrl { get; }
    public List<HttpListenerRequest> Requests { get; } = new();

    public LoopbackServer(Action<HttpListenerRequest, HttpListenerResponse> handler)
    {
        BaseUrl = $"http://127.0.0.1:{FreePort()}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();
        _loop = Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try { context = await _listener.GetContextAsync(); }
                catch { return; }

                lock (Requests) Requests.Add(context.Request);
                handler(context.Request, context.Response);
                context.Response.Close();
            }
        });
    }

    public static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    public void Dispose()
    {
        _listener.Close();
        try { _loop.Wait(1000); } catch { /* shutting down */ }
    }
}
