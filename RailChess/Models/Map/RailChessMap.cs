using Newtonsoft.Json;

namespace RailChess.Models.Map
{
    public class RailChessMap
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int Author { get; set; }
        public string? TopoData { get; set; }
        public string? ImgFileName { get; set; }
        public int StationCount { get; set; }
        public int ExcStationCount { get; set; }
        public int TotalDirections { get; set; }
        public int LineCount { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool Deleted { get; set; }
    }
}
