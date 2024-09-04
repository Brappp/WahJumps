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

            foreach (var dataCenter in dataCenters)
            {
                StatusUpdated?.Invoke($"Processing sheet: {dataCenter.DataCenter}");

                var csvData = await DownloadCsv(dataCenter.Url);

                if (csvData == null)
                {
                    StatusUpdated?.Invoke($"Failed to download CSV for {dataCenter.DataCenter}");
                    continue;
                }

                var cleanedData = CleanCsvData(csvData);
                SaveCsv(cleanedData, Path.Combine(outputDirectory, $"{dataCenter.CsvName}_cleaned.csv"));
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
    }
}
