using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core.Test
{
    [TestClass]
    public class ExclusiveStasFinderTest
    {
        private readonly IExclusiveStasFinder _finder;
        public ExclusiveStasFinderTest()
        {
            _finder = new ExclusiveStasFinder();

            //测试中关掉超时机制，以便断点调试
            //ExclusiveStasFinder.DisableTimeoutTestOnly = true;
        }
        [TestMethod]
        public void Simple()
        {
            //   3
            //   |\
            // 1-2-4-5
            //     |
            //     6
            var sta1 = new Sta(1, 1);
            var sta2 = new Sta(2, 1);
            var sta3 = new Sta(3, 0);
            var sta4 = new Sta(4, 2);
            var sta5 = new Sta(5, 0);
            var sta6 = new Sta(6, 0);
            Dictionary<int, int> playerPosition = new()
            {
                { 1, 2 }, { 2, 4 }
            };
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6
            },playerPosition);
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta3.TwowayConnect(sta4);
            sta2.TwowayConnect(sta4);
            sta4.TwowayConnect(sta5);
            sta4.TwowayConnect(sta6);

            var exs1 = _finder.FindExclusiveStas(graph, 2).ToList();
            CollectionAssert.AreEquivalent(new List<int> { 4, 5, 6 }, exs1);

            var exs2 = _finder.FindExclusiveStas(graph, 1).ToList();
            CollectionAssert.AreEquivalent(new List<int> { 1, 2 }, exs2);
        }

        [TestMethod]
        public void Combined1()
        {
            // 1-2-4
            //   | |
            //   3-5-6
            //     |
            //     7
            var sta1 = new Sta(1, 0);
            var sta2 = new Sta(2, 1);
            var sta3 = new Sta(3, 0);
            var sta4 = new Sta(4, 0);
            var sta5 = new Sta(5, 2);
            var sta6 = new Sta(6, 0);
            var sta7 = new Sta(7, 0);
            Dictionary<int, int> playerPosition = new()
            {
                { 1, 2 }, { 2, 5 }
            };
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6, sta7
            },playerPosition);
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta2.TwowayConnect(sta4);
            sta3.TwowayConnect(sta5);
            sta4.TwowayConnect(sta5);
            sta5.TwowayConnect(sta6);
            sta5.TwowayConnect(sta7);

            var paths2 = _finder.FindExclusiveStas(graph, 1).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 1, 2 }, paths2);

            var paths3 = _finder.FindExclusiveStas(graph, 2).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 5, 6, 7 }, paths3);
        }

        [TestMethod]
        public void Combined2()
        {
            // 1-2-4
            //   | |
            //   3-5-6
            //     |
            //     7
            var sta1 = new Sta(1, 0);
            var sta2 = new Sta(2, 1);
            var sta3 = new Sta(3, 0);
            var sta4 = new Sta(4, 0);
            var sta5 = new Sta(5, 1);
            var sta6 = new Sta(6, 2);
            var sta7 = new Sta(7, 0);
            Dictionary<int, int> playerPosition = new()
            {
                { 1, 2 }, { 2, 6 }
            };
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6, sta7
            }, playerPosition);
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta2.TwowayConnect(sta4);
            sta3.TwowayConnect(sta5);
            sta4.TwowayConnect(sta5);
            sta5.TwowayConnect(sta6);
            sta5.TwowayConnect(sta7);

            var paths2 = _finder.FindExclusiveStas(graph, 1).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 1, 2, 3, 4, 5, 7}, paths2);

            var paths3 = _finder.FindExclusiveStas(graph, 2).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 6 }, paths3);
        }

        [TestMethod]
        public void PlayersOverlapped()
        {
            // 1-2-3
            //   | |
            // 4-5-6
            // | | |
            // 7-8-9-10
            var sta1 = new Sta(1, 0);
            var sta2 = new Sta(2, 1);
            var sta3 = new Sta(3, 3);
            var sta4 = new Sta(4, 0);
            var sta5 = new Sta(5, 0);
            var sta6 = new Sta(6, 3);
            // 历史bug：
            // 玩家1在位置2，为玩家1计算Exclusive车站时，
            // 位置6被玩家3占了，导致位于7的2号玩家无法到达3，但可以到达9（加入了"otherReachable" HashSet），
            // 导致位于10的3号玩家一出门就被判定为无法占领更多点，结束计算，没算入其能到达的3号位置
            // 最终导致玩家1错误地占领了位置3（目前已修复）
            var sta7 = new Sta(7, 2);
            var sta8 = new Sta(8, 0);
            var sta9 = new Sta(9, 0);
            var sta10 = new Sta(10, 3);
            Dictionary<int, int> playerPosition = new()
            {
                { 1, 2 }, { 2, 7 }, { 3, 10 }
            };
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6, sta7, sta8, sta9, sta10
            }, playerPosition);
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);

            sta2.TwowayConnect(sta5);
            sta3.TwowayConnect(sta6);

            sta4.TwowayConnect(sta5);
            sta5.TwowayConnect(sta6);

            sta4.TwowayConnect(sta7);
            sta5.TwowayConnect(sta8);
            sta6.TwowayConnect(sta9);

            sta7.TwowayConnect(sta8);
            sta8.TwowayConnect(sta9);
            sta9.TwowayConnect(sta10);

            var res = _finder.FindExclusiveStas(graph, 1).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 1, 2 }, res);
        }
    }
}
