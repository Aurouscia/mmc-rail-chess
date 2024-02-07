namespace RailChess.Models.COM
{
    public class QuickSearchResult
    {
        public List<QuickSearchResultItem> Items { get; set; }
        public QuickSearchResult()
        {
            Items = new();
        }
        public class QuickSearchResultItem
        {
            public string Name { get; set; }
            public string? Desc { get; set; }
            public int Id { get; set; }
            public QuickSearchResultItem(string name, string? desc, int id)
            {
                Name = name;
                Desc = desc;
                Id = id;
            }
        }
    }
}
