using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Dalamud.Plugin.Services;
using WahJumps.Models;
using WahJumps.Data;

namespace WahJumps.Handlers
{
    public class CsvManager
    {
        public event Action<string>? StatusUpdated;
        public event Action<float>? ProgressUpdated;
        public event Action? CsvProcessingCompleted;

        private readonly string outputDirectory;
        private readonly IChatGui chatGui;

        public CsvManager(IChatGui chatGui, string outputDirectory)
        {
            this.chatGui = chatGui;
            this.outputDirectory = outputDirectory;
        }

        public string CsvDirectoryPath => outputDirectory;

        public void DeleteExistingCsvs()
        {
            try
            {
                var files = Directory.GetFiles(outputDirectory, "*.csv");

                foreach (var file in files)
                {
                    File.Delete(file);
                    StatusUpdated?.Invoke($"Deleted old CSV: {file}");
                }
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Error deleting old CSVs: {ex.Message}");
            }
        }

        public async Task DownloadAndSaveIndividualCsvsAsync()
        {
            var dataCenters = WorldData.GetDataCenterInfo();
            int totalDataCenters = dataCenters.Count;
            int processedCount = 0;

            StatusUpdated?.Invoke($"Processing 0/{totalDataCenters} data centers...");
            ProgressUpdated?.Invoke(0f); // Start with 0 progress

            foreach (var dataCenter in dataCenters)
            {
                StatusUpdated?.Invoke($"Processing {dataCenter.DataCenter} ({processedCount + 1}/{totalDataCenters})");

                var csvData = await DownloadCsv(dataCenter.Url);

                if (csvData == null)
                {
                    StatusUpdated?.Invoke($"Failed to download CSV for {dataCenter.DataCenter}");
                    processedCount++;

                    // Update progress even on failure
                    float progress = (float)processedCount / totalDataCenters;
                    ProgressUpdated?.Invoke(progress);

                    continue;
                }

                // Preprocess the CSV data to ensure the "ID" column is present
                var preprocessedCsv = PreprocessCsvForMissingId(csvData);

                var cleanedData = CleanCsvData(preprocessedCsv);
                SaveCsv(cleanedData, Path.Combine(outputDirectory, $"{dataCenter.CsvName}_cleaned.csv"));

                processedCount++;

                // Update progress
                float progressValue = (float)processedCount / totalDataCenters;
                ProgressUpdated?.Invoke(progressValue);
            }

            CsvProcessingCompleted?.Invoke();
        }

        private async Task<string?> DownloadCsv(string url)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync(url);
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

            using (var reader = new StringReader(csvData))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Context.RegisterClassMap<JumpPuzzleDataMap>();
                var records = csv.GetRecords<JumpPuzzleData>().ToList();

                foreach (var record in records)
                {
                    cleanedData.Add(record);
                }
            }

            return cleanedData;
        }

        private void SaveCsv(IEnumerable<JumpPuzzleData> data, string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
                csv.WriteRecords(data);
                StatusUpdated?.Invoke($"Successfully saved cleaned CSV to: {filePath}");
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Error saving cleaned CSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Preprocesses the CSV data to ensure the "ID" column is present.
        /// </summary>
        private string PreprocessCsvForMissingId(string csvData)
        {
            var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                throw new Exception("CSV data is empty.");
            }

            // Check if the first line contains an "ID" column
            var headers = lines[0].Split(',');
            if (!headers.Contains("ID"))
            {
                StatusUpdated?.Invoke("ID column missing, adding it dynamically.");

                // Add "ID" to the header and prepend ID values to each subsequent row
                var processedLines = new List<string> { "ID," + lines[0] };
                for (int i = 1; i < lines.Length; i++)
                {
                    processedLines.Add($"{i},{lines[i]}");
                }

                return string.Join(Environment.NewLine, processedLines);
            }

            return csvData; // No modification needed if "ID" exists
        }
    }
}
