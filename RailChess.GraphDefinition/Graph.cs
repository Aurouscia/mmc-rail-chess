namespace RailChess.GraphDefinition
{
    public class Graph
    {
        public List<Sta> Stations { get; }
        public Dictionary<int, List<int>> Lines { get; }
        /// <summary>
        /// 玩家位置<br/>
        /// 与其他两个不变的不同，这个应根据对局的演进重新设置
        /// </summary>
        public Dictionary<int, int> UserPosition { get; }
        /// <summary>
        /// 构造图
        /// </summary>
        /// <param name="stations">车站</param>
        /// <param name="lines">线路（可不提供）</param>
        public Graph(
            List<Sta> stations, Dictionary<int, List<int>>? lines = null)
        {
            Stations = stations;
            UserPosition = [];
            Lines = lines ?? [];
        }
        /// <summary>
        /// 构造图，单元测试专用（直接设置玩家位置）
        /// </summary>
        /// <param name="stations">车站</param>
        /// <param name="userPosition">玩家位置</param>
        /// <param name="lines">线路（可不提供）</param>
        public Graph(
            List<Sta> stations, Dictionary<int, int> userPosition,
            Dictionary<int, List<int>>? lines = null)
        {
            Stations = stations;
            UserPosition = userPosition;
            Lines = lines ?? [];
        }
    }
    /// <summary>
    /// 带有线路id的站点<br/>
    /// 直观理解：指的是某一个站台（例如：人民广场站1号线部分、人民广场站8号线部分）
    /// </summary>
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
