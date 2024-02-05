namespace RailChess.Models.Map
{
    public class RailChessTopo
    {
        public List<Sta>? Stations { get; set; }
        public List<Line>? Lines { get; set; }

        public int MaxStationId()
        {
            if (Stations is null) return -1;
            var s = Stations.MaxBy(x => x.Id);
            if (s == null) return -1;
            return s.Id;
        }
        public int StationDirections(int stationId)
        {
            int res = 0;
            if (Lines is null || Stations is null)
                return 0;
            foreach (Line l in Lines)
            {
                if (l.Stas is null)
                    continue;
                int idx = l.Stas.IndexOf(stationId);
                if (idx == 0 || idx == Stations.Count - 1)
                {
                    res += 1;
                }
                else if (idx != -1)
                {
                    res += 2;
                }
            }
            return res;
        }

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
