using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RePlays.Classes.Utils {
    public static class WebServer {
        static IWebHost server;
        static bool isRunning;
        static List<WebSocket> activeSockets = [];

        // 3001 is a very common dev port, so the default is an uncommon one. Set REPLAYS_WEB_PORT
        // (1024-65535) to use another; the UI is told which port to connect to when it is opened.
        public const int DefaultPort = 38417;
        public static readonly int Port = ReadPort();

        static int ReadPort() {
            var value = Environment.GetEnvironmentVariable("REPLAYS_WEB_PORT");
            if (int.TryParse(value, out int port) && port is >= 1024 and <= 65535) return port;
            if (!string.IsNullOrEmpty(value)) Logger.WriteLine($"Ignoring REPLAYS_WEB_PORT '{value}', using {DefaultPort}");
            return DefaultPort;
        }

        public static void Start() {
            if (isRunning) {
                return;
            }
#if RELEASE
            string webRootDir = Path.Join(Functions.GetStartupPath(), "/ClientApp/build/");
#else
            string webRootDir = Path.Join(Functions.GetSolutionPath(), "/ClientApp/build/");
#endif
            if (!Path.Exists(webRootDir)) webRootDir = Functions.GetPlaysFolder();

            server = WebHost.CreateDefaultBuilder([$"--urls=http://localhost:{Port}/"])
                .Configure(app => {
                    // Serve videos
                    app.UseStaticFiles(new StaticFileOptions {
                        ServeUnknownFileTypes = true,
                        FileProvider = new PhysicalFileProvider(Functions.GetPlaysFolder()),
                    });

                    app.UseFileServer();

                    // Enable WebSocket support
                    app.UseWebSockets();

                    // Map WebSocket endpoint
                    app.Use(async (context, next) => {
                        if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest) {
                            if (!IsAllowedOrigin(context.Request.Headers["Origin"].ToString())) {
                                Logger.WriteLine($"Rejected websocket from origin '{context.Request.Headers["Origin"]}'");
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                return;
                            }
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            activeSockets.Add(webSocket);
                            await HandleWebSocket(context, webSocket);
                        }
                        else await next();
                    });
                })
                .UseWebRoot(webRootDir).Build();
            server.RunAsync();
            isRunning = true;
            Logger.WriteLine("Local web server started with WebRoot dir: " + webRootDir);
        }

        public static void Stop() {
            server?.StopAsync();
            isRunning = false;
        }

        public static List<WebSocket> GetActiveSockets() {
            return [.. activeSockets];
        }

        // The app's own pages load from file:// (release) or localhost (dev), which show up as
        // "null"/"file://" or localhost origins. Anything else is a web page in the user's browser
        // trying to talk to the app, so it gets turned away. A missing Origin is a non-browser client.
        static bool IsAllowedOrigin(string origin) {
            if (string.IsNullOrEmpty(origin)) return true;
            return origin is "null" or "file://" or "http://localhost:3000" || origin == $"http://localhost:{Port}";
        }

        private static async Task HandleWebSocket(HttpContext _, WebSocket webSocket) {
            var buffer = new byte[1024 * 4];
            using var message = new MemoryStream();

            try {
                while (webSocket.State == WebSocketState.Open) {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text) {
                        // a message can arrive in several frames, keep reading until it ends
                        message.Write(buffer, 0, result.Count);
                        if (!result.EndOfMessage) continue;
                        var receivedMessage = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                        message.SetLength(0);
#if !WINDOWS
                        await WebMessage.ReceiveMessage(receivedMessage);
#endif
                    }
                    else if (result.MessageType == WebSocketMessageType.Close) {
                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    }
                }
            }
            catch (WebSocketException ex) {
                Logger.WriteLine($"Websocket closed: {ex.Message}");
            }
            finally {
                activeSockets.Remove(webSocket);
            }
        }
    }
}
