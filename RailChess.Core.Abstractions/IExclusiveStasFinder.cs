using RailChess.GraphDefinition;

namespace RailChess.Core.Abstractions
{
    public interface IExclusiveStasFinder
    {
        public IEnumerable<int> FindExclusiveStas(Graph graph, int userId);
    }
}
