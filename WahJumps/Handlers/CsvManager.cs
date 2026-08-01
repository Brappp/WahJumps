using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Dalamud.Plugin.Services;
using WahJumps.Models;
using WahJumps.Data;

namespace WahJumps.Handlers
{
    public class CsvManager : IDisposable
    {
        public event Action<string>? StatusUpdated;
        public event Action<float>? ProgressUpdated;
        public event Action? CsvProcessingCompleted;

        private readonly string outputDirectory;
        private readonly IChatGui chatGui;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public CsvManager(IChatGui chatGui, string outputDirectory)
        {
            this.chatGui = chatGui;
            this.outputDirectory = outputDirectory;
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        public string CsvDirectoryPath => outputDirectory;

        public async Task DownloadAndSaveIndividualCsvsAsync()
        {
            var dataCenters = WorldData.GetDataCenterInfo();
            int totalDataCenters = dataCenters.Count;
            int processedCount = 0;

            StatusUpdated?.Invoke($"Processing 0/{totalDataCenters} data centers...");
            ProgressUpdated?.Invoke(0f);

            try
            {
                foreach (var dataCenter in dataCenters)
                {
                    if (cts.IsCancellationRequested) break;

                    StatusUpdated?.Invoke($"Processing {dataCenter.DataCenter} ({processedCount + 1}/{totalDataCenters})");

                    try
                    {
                        var csvData = await DownloadCsv(dataCenter.Url);

                        if (csvData == null)
                        {
                            StatusUpdated?.Invoke($"Failed to download {dataCenter.DataCenter}; keeping existing data");
                        }
                        else
                        {
                            var preprocessedCsv = PreprocessCsvForMissingId(csvData);
                            var cleanedData = CleanCsvData(preprocessedCsv);
                            SaveCsv(cleanedData, Path.Combine(outputDirectory, $"{dataCenter.CsvName}_cleaned.csv"));
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusUpdated?.Invoke($"Error processing {dataCenter.DataCenter}: {ex.Message}; keeping existing data");
                    }

                    processedCount++;
                    ProgressUpdated?.Invoke((float)processedCount / totalDataCenters);
                }
            }
            finally
            {
                CsvProcessingCompleted?.Invoke();
            }
        }

        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        private async Task<string?> DownloadCsv(string url)
        {
            try
            {
                var response = await httpClient.GetStringAsync(url, cts.Token);
                StatusUpdated?.Invoke($"Successfully downloaded CSV from: {url}");
                return response;
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Error downloading CSV: {ex.Message}");
                return null;
            }
        }

        private List<JumpPuzzleData> CleanCsvData(string csvData)
        {
            var cleanedData = new List<JumpPuzzleData>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using (var reader = new StringReader(csvData))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<JumpPuzzleDataMap>();
                cleanedData.AddRange(csv.GetRecords<JumpPuzzleData>());
            }

            return cleanedData;
        }

        private void SaveCsv(IEnumerable<JumpPuzzleData> data, string filePath)
        {
            var tempPath = filePath + ".tmp";
            try
            {
                using (var writer = new StreamWriter(tempPath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    csv.WriteRecords(data);
                }

                File.Move(tempPath, filePath, overwrite: true);
                StatusUpdated?.Invoke($"Successfully saved cleaned CSV to: {filePath}");
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Error saving cleaned CSV: {ex.Message}");
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }

        private string PreprocessCsvForMissingId(string csvData)
        {
            var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                throw new Exception("CSV data is empty.");
            }

            var headers = lines[0].Split(',');
            if (!headers.Contains("ID"))
            {
                StatusUpdated?.Invoke("ID column missing, adding it dynamically.");

                var processedLines = new List<string> { "ID," + lines[0] };
                for (int i = 1; i < lines.Length; i++)
                {
                    processedLines.Add($"{i},{lines[i]}");
                }

                return string.Join(Environment.NewLine, processedLines);
            }

            return csvData;
        }
    }
}
