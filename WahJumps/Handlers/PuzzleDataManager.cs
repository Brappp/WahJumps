using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WahJumps.Models;

namespace WahJumps.Handlers
{
    public class PuzzleDataSnapshot
    {
        public string Version { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, List<JumpPuzzleData>> DataCenters { get; set; } = new();
    }

    public class PuzzleDataManager : IDisposable
    {
        private const string ManifestUrl = "https://raw.githubusercontent.com/wahtf/WahJumps/data/manifest.json";
        private const string PuzzlesUrl = "https://raw.githubusercontent.com/wahtf/WahJumps/data/puzzles.json";
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        public event Action<string>? StatusUpdated;
        public event Action? DataUpdated;

        private readonly string cacheFilePath;
        private readonly CancellationTokenSource cts = new();
        private int checkRunning;
        private DateTime lastCheck = DateTime.MinValue;

        public string DataDirectory { get; }
        public PuzzleDataSnapshot? Snapshot { get; private set; }

        public PuzzleDataManager(string dataDirectory)
        {
            DataDirectory = dataDirectory;
            cacheFilePath = Path.Combine(dataDirectory, "puzzles.json");
            TryLoadCache();
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        public void RequestCheck(bool force = false)
        {
            if (!force && DateTime.UtcNow - lastCheck < CheckInterval) return;
            if (Interlocked.CompareExchange(ref checkRunning, 1, 0) != 0) return;

            lastCheck = DateTime.UtcNow;

            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckForUpdates();
                }
                catch (Exception ex)
                {
                    StatusUpdated?.Invoke($"Update check failed: {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref checkRunning, 0);
                }
            });
        }

        private async Task CheckForUpdates()
        {
            StatusUpdated?.Invoke("Checking for puzzle data updates...");

            string manifestJson = await httpClient.GetStringAsync(ManifestUrl, cts.Token);
            var manifest = JsonConvert.DeserializeObject<PuzzleDataSnapshot>(manifestJson);
            if (manifest == null || string.IsNullOrEmpty(manifest.Version))
            {
                StatusUpdated?.Invoke("Update check failed: invalid manifest");
                return;
            }

            if (Snapshot != null && manifest.Version == Snapshot.Version)
            {
                StatusUpdated?.Invoke("Ready");
                return;
            }

            StatusUpdated?.Invoke("Downloading puzzle data...");
            string dataJson = await httpClient.GetStringAsync(PuzzlesUrl, cts.Token);
            var snapshot = JsonConvert.DeserializeObject<PuzzleDataSnapshot>(dataJson);
            if (snapshot == null || snapshot.DataCenters.Count == 0)
            {
                StatusUpdated?.Invoke("Update failed: invalid puzzle data");
                return;
            }

            var tempPath = cacheFilePath + ".tmp";
            File.WriteAllText(tempPath, dataJson);
            File.Move(tempPath, cacheFilePath, overwrite: true);

            Snapshot = snapshot;
            StatusUpdated?.Invoke("Ready");
            DataUpdated?.Invoke();
        }

        private void TryLoadCache()
        {
            try
            {
                if (File.Exists(cacheFilePath))
                {
                    var snapshot = JsonConvert.DeserializeObject<PuzzleDataSnapshot>(File.ReadAllText(cacheFilePath));
                    if (snapshot != null && snapshot.DataCenters.Count > 0)
                    {
                        Snapshot = snapshot;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Warning($"Failed to load cached puzzle data: {ex.Message}");
            }
        }
    }
}
