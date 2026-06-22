using RailChess.GraphDefinition;

namespace RailChess.Core.Abstractions
{
    public interface IExclusiveStasFinder
    {
        public List<int> FindExclusiveStas(Graph graph, int userId);
    }
}
