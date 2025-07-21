using RailChess.Models.Game;
using RailChess.Play.Services.Core;

namespace RailChess.Utils
{
    public static class SpawnRule
    {
        public static List<int> Spawn(
            CoreGraphProvider coreGraphProvider,
            SpawnRuleType type)
        {
            return type switch
            {
                SpawnRuleType.TwinExchange => coreGraphProvider.TwinExchanges(),
                _ => coreGraphProvider.PureTerminals()
            };
        }
        public static string SpawnRuleTypeName(SpawnRuleType type)
        {
            return type switch
            {
                SpawnRuleType.Terminal => "终点站",
                SpawnRuleType.TwinExchange => "双线换乘站",
                _ => "???"
            };
        }
    }
}
