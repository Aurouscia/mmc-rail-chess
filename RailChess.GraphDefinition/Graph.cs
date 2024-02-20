namespace RailChess.GraphDefinition
{
    public class Graph
    {
        public List<Sta> Stations { get; }
        public Dictionary<int, int> UserPosition { get; }
        public Graph(List<Sta> stations)
        {
            Stations = stations;
            UserPosition = new();
        }
        public Graph(List<Sta> stations, Dictionary<int, int> userPosition)
        {
            Stations = stations;
            UserPosition = userPosition;
        }
    }
    public class LinedSta
    {
        public int LineId { get; set; }
        public Sta Station { get; set; }
        public LinedSta(int lineId, Sta station)
        {
            LineId = lineId;
            Station = station;
        }
        public override bool Equals(object? obj)
        {
            if (obj is LinedSta otherSta)
            {
                return this.Station.Id == otherSta.Station.Id && this.LineId == otherSta.LineId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.Station.Id.GetHashCode() + this.LineId.GetHashCode();
        }
    }
    public class Sta
    {
        public int Id { get; }
        public int Owner { get; set; }
        public List<LinedSta> Neighbors { get; private set; }
        public Sta(int id)
        {
            Id = id;
            Owner = 0;
            Neighbors = new(2);
        }
        public Sta(int id, int owner)
        {
            Id = id;
            Owner = owner;
            Neighbors = new(2);
        }

        public void TwowayConnect(Sta other, int line = 0)
        {
            if (other.Id != this.Id)
            {
                if(!this.Neighbors.Any(x=>x.Station.Id == other.Id && x.LineId == line))
                    this.Neighbors.Add(new(line, other));
                if(!other.Neighbors.Any(x=>x.Station.Id == this.Id && x.LineId == line))
                    other.Neighbors.Add(new(line, this));
            }
        }

        public override string ToString()
        {
            return $"Sta_{this.Id}";
        }
    }
}
