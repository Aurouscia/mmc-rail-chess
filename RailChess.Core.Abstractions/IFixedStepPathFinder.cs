using RailChess.GraphDefinition;

namespace RailChess.Core.Abstractions
{
    public interface IFixedStepPathFinder
    {
        public IEnumerable<IEnumerable<int>> FindAllPaths(Graph graph, int userId, int steps);
        public bool IsValidMove(Graph graph, int userId, int from, int to, int steps);
    }
}
