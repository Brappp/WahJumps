using CsvHelper.Configuration;

namespace WahJumps.Models
{
    public sealed class JumpPuzzleDataMap : ClassMap<JumpPuzzleData>
    {
        public JumpPuzzleDataMap()
        {
            Map(m => m.Id).Name("ID");
            Map(m => m.Rating).Name("Rating");
            Map(m => m.PuzzleName).Name("Puzzle Name");
            Map(m => m.Builder).Name("Builder");
            Map(m => m.World).Name("World");
            Map(m => m.Address).Name("Address");
            Map(m => m.M).Name("M");
            Map(m => m.E).Name("E");
            Map(m => m.S).Name("S");
            Map(m => m.P).Name("P");
            Map(m => m.V).Name("V");
            Map(m => m.J).Name("J");
            Map(m => m.G).Name("G");
            Map(m => m.L).Name("L");
            Map(m => m.X).Name("X");
            Map(m => m.GoalsOrRules).Name("Goals/Rules");  
        }
    }
}
