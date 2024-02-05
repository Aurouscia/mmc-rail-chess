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
                if (l.Stas is null || l.Stas.Count==0)
                    continue;
                for(int i=0;i<l.Stas.Count;i++)
                { 
                    var s = l.Stas[i];
                    if (s == stationId)
                    {
                        if (i == 0 || i == l.Stas.Count - 1)
                            res += 1;
                        else
                            res += 2;
                    }
                }
            }
            return res;
        }
        public Dictionary<int,int> StationsDirections()
        {
            Dictionary<int, int> res = new();
            if (Lines is null || Stations is null)
                return res;
            foreach (Line l in Lines)
            {
                if (l.Stas is null || l.Stas.Count == 0)
                    continue;
                for (int i = 0; i < l.Stas.Count; i++)
                {
                    var s = l.Stas[i];
                    if (res.ContainsKey(s))
                    {
                        if (i == 0 || i == l.Stas.Count - 1)
                            res[s] += 1;
                        else
                            res[s] += 2;
                    }
                    else 
                    {
                        if (i == 0 || i == l.Stas.Count - 1)
                            res.Add(s, 1);
                        else
                            res.Add(s, 2);
                    }
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
