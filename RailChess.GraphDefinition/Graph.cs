namespace RailChess.GraphDefinition
{
    public class Graph
    {
        public int OpEventId { get; set; }
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

        /// <summary>
        /// 已完成构造图的收尾工作
        /// </summary>
        public bool BuildingCompleted { get; private set; } = false;
        /// <summary>
        /// Dictionary[线路号, Dictionary[车站号, List[车站在线路中的索引]]]<br/>
        /// 在<see cref="CompleteBuilding"/>中被设置
        /// </summary>
        public Dictionary<int, Dictionary<int, List<int>>>? LineStaIndexes { get; private set; } = null;
        /// <summary>
        /// 构造图的收尾工作，在进行了所有<see cref="Sta.TwowayConnect"/>之后调用一次<br/>
        /// 会为算出所有站在其在线路中的索引，赋值给<see cref="LineStaIndexes"/><br/>
        /// 并使用其值填充<see cref="LinedSta.Indexes"/>
        /// </summary>
        public void CompleteBuilding()
        {
            if (BuildingCompleted)
                return;
            if (Lines.Count == 0)
                return; //未提供线路信息（单元测试环境）
            LineStaIndexes = [];
            foreach(var line in Lines)
            {
                var lineId = line.Key;
                var stas = line.Value;
                Dictionary<int, List<int>> staIndexesHere = [];
                for(int i = 0; i < stas.Count; i++)
                {
                    var s = stas[i];
                    bool found = staIndexesHere.TryGetValue(s, out var indexes);
                    indexes ??= [];
                    indexes.Add(i);
                    if(!found)
                        staIndexesHere.Add(s, indexes);
                }
                LineStaIndexes.Add(lineId, staIndexesHere);
            }
            foreach(var s in Stations)
            {
                foreach(var n in s.Neighbors)
                {
                    if(n.LineId > 0)
                        n.Indexes = LineStaIndexes[n.LineId][n.Station.Id];
                }
            }
            BuildingCompleted = true;
        }
    }
    /// <summary>
    /// 带有线路id的站点<br/>
    /// 直观理解：指的是某一个站台（例如：人民广场站1号线部分、人民广场站8号线部分）
    /// </summary>
    public class LinedSta(int lineId, Sta station)
    {
        public int LineId { get; } = lineId;
        public Sta Station { get; } = station;
        /// <summary>
        /// 车站在<see cref="LineId"/>所指线路中的索引，可能有多个<br/>
        /// 该索引在<see cref="Graph.CompleteBuilding"/>中被自动设置
        /// </summary>
        public List<int>? Indexes { get; set; } = null;
        public override string ToString()
        {
            return $"{Station}_on_line{LineId}";
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
