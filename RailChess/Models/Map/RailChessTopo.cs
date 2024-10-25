namespace RailChess.Models.Map
{
    public class RailChessTopo
    {
        public List<Sta>? Stations { get; set; }
        public List<Line>? Lines { get; set; }

        public class Sta : List<int>
        {
            public int Id => this[0];
            public int X => this[1];
            public int Y => this[2];
        }
        public class Line
        {
            public int Id { get; set; }
            public List<int>? Stas { get; set; }
        }
    }
}
