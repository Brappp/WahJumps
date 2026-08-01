using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using WahJumps.DataTool;
using WahJumps.Models;

string outDir = args.Length > 0 ? args[0] : "dist";
Directory.CreateDirectory(outDir);

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
var dataCenters = new SortedDictionary<string, List<JumpPuzzleData>>(StringComparer.Ordinal);

foreach (var source in SheetSources.All)
{
    Console.WriteLine($"Downloading {source.DataCenter}...");
    string csvText = await http.GetStringAsync(source.Url);
    var puzzles = ParseCsv(EnsureIdColumn(csvText));

    if (puzzles.Count == 0)
    {
        Console.Error.WriteLine($"{source.DataCenter} produced no puzzles; aborting without publishing.");
        return 1;
    }

    puzzles.Sort((a, b) => a.Id.CompareTo(b.Id));
    dataCenters[source.DataCenter] = puzzles;
    Console.WriteLine($"  {puzzles.Count} puzzles");
}

byte[] dataBytes = JsonSerializer.SerializeToUtf8Bytes(dataCenters);
string version = Convert.ToHexString(SHA256.HashData(dataBytes))[..12].ToLowerInvariant();
string generatedAt = DateTime.UtcNow.ToString("O");

var allPuzzles = dataCenters.Values.SelectMany(p => p).ToList();
var manifest = new
{
    Version = version,
    GeneratedAt = generatedAt,
    PuzzleCount = allPuzzles.Count,
    BuilderCount = allPuzzles.Where(p => !string.IsNullOrEmpty(p.Builder)).Select(p => p.Builder).Distinct().Count(),
    WorldCount = allPuzzles.Select(p => p.World).Distinct().Count()
};
var payload = new
{
    Version = version,
    GeneratedAt = generatedAt,
    DataCenters = dataCenters
};

File.WriteAllText(Path.Combine(outDir, "puzzles.json"), JsonSerializer.Serialize(payload));
File.WriteAllText(Path.Combine(outDir, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"Version {version}: {manifest.PuzzleCount} puzzles, {manifest.BuilderCount} builders, {manifest.WorldCount} worlds");
return 0;

static string EnsureIdColumn(string csvData)
{
    var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length == 0)
    {
        throw new Exception("CSV data is empty.");
    }

    var headers = lines[0].Split(',');
    if (headers.Contains("ID"))
    {
        return csvData;
    }

    var processedLines = new List<string> { "ID," + lines[0] };
    for (int i = 1; i < lines.Length; i++)
    {
        processedLines.Add($"{i},{lines[i]}");
    }

    return string.Join(Environment.NewLine, processedLines);
}

static List<JumpPuzzleData> ParseCsv(string csvText)
{
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null
    };

    using var reader = new StringReader(csvText);
    using var csv = new CsvReader(reader, config);
    csv.Context.RegisterClassMap<PuzzleCsvMap>();
    return csv.GetRecords<JumpPuzzleData>().ToList();
}
