using System.Collections.Generic;

namespace WahJumps.Data
{
    public class DataCenterInfo
    {
        public string DataCenter { get; set; }
        public string Url { get; set; }
        public string CsvName { get; set; }
    }

    public static class WorldData
    {
        public static List<DataCenterInfo> GetDataCenterInfo()
        {
            return new List<DataCenterInfo>()
            {
                // NA
                new DataCenterInfo { DataCenter = "Aether", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=82382952", CsvName = "aether" },
                new DataCenterInfo { DataCenter = "Primal", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1186977950", CsvName = "primal" },
                new DataCenterInfo { DataCenter = "Crystal", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=350373672", CsvName = "crystal" },
                new DataCenterInfo { DataCenter = "Dynamis", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1383994086", CsvName = "dynamis" },

                // EU
                new DataCenterInfo { DataCenter = "Chaos", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1339692877", CsvName = "chaos" },
                new DataCenterInfo { DataCenter = "Light", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=175977131", CsvName = "light" },

                // OCE
                new DataCenterInfo { DataCenter = "Materia", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=874557131", CsvName = "materia" },

                // JP
                new DataCenterInfo { DataCenter = "Elemental", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1856583868", CsvName = "elemental" },
                new DataCenterInfo { DataCenter = "Gaia", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1822506732", CsvName = "gaia" },
                new DataCenterInfo { DataCenter = "Mana", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1045300014", CsvName = "mana" },
                new DataCenterInfo { DataCenter = "Meteor", Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1643199164", CsvName = "meteor" }
            };
        }
    }
}
