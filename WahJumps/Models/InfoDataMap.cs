using CsvHelper.Configuration;

namespace WahJumps.Models
{
    public sealed class InfoDataMap : ClassMap<InfoData>
    {
        public InfoDataMap()
        {
            Map(m => m.Section).Index(0);
            Map(m => m.Key).Index(1);
            Map(m => m.Value1).Index(2);
            Map(m => m.Value2).Index(3);
            Map(m => m.Value3).Index(4);
        }
    }
} 