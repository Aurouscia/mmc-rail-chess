using RailChess.GraphDefinition;

namespace RailChess.Play.Services.Core
{
    public class CoreGraphEvaluator
    {
        public Dictionary<int, int> StationDirections(Graph graph)
        {
            Dictionary<int, int> dict = [];
            graph.Stations.ForEach(x =>
            {
                var count = x.Neighbors.Select(x => x.Station).Distinct().Count();
                dict.Add(x.Id, count);
            });
            return dict;
        }
    }
}
