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
    public class Sta
    {
        public int Id { get; }
        public int Owner { get; set; }
        public List<Sta> Neighbors { get; private set; }
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
        public Sta(int id, int owner, List<Sta> neighbors)
        {
            Id = id;
            Owner = owner;
            Neighbors = neighbors;
        }

        public void TwowayConnect(Sta other)
        {
            if (other.Id != this.Id)
            {
                if(!this.Neighbors.Any(x=>x.Id == other.Id))
                    this.Neighbors.Add(other);
                if(!other.Neighbors.Any(x=>x.Id == this.Id))
                    other.Neighbors.Add(this);
            }
        }

        public override string ToString()
        {
            return $"Sta_{this.Id}";
        }
    }
}
