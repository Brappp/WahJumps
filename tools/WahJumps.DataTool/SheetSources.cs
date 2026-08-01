namespace WahJumps.DataTool
{
    public sealed record SheetSource(string DataCenter, string Url);

    public static class SheetSources
    {
        public static readonly SheetSource[] All =
        {
            new("Aether", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=82382952"),
            new("Primal", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1186977950"),
            new("Crystal", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=350373672"),
            new("Dynamis", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1383994086"),
            new("Chaos", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1339692877"),
            new("Light", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=175977131"),
            new("Materia", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=874557131"),
            new("Elemental", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1856583868"),
            new("Gaia", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1822506732"),
            new("Mana", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1045300014"),
            new("Meteor", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1643199164"),
        };
    }
}
