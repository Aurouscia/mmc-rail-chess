using Microsoft.VisualStudio.TestTools.UnitTesting;
using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core.Test
{
    [TestClass]
    public class FixedStepPathFinderTest
    {
        private readonly IFixedStepPathFinder _finder;
        public FixedStepPathFinderTest()
        {
            _finder = new FixedStepPathFinder();
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
            var sta3 = new Sta(3, 1);
            var sta4 = new Sta(4, 1);
            var sta5 = new Sta(5, 1);
            var sta6 = new Sta(6, 1);
            var playerPos = new Dictionary<int, int>()
            {
                { 1, 1 }
            };
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6
            }, playerPos);
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta3.TwowayConnect(sta4);
            sta2.TwowayConnect(sta4);
            sta4.TwowayConnect(sta5);
            sta4.TwowayConnect(sta6);

            var paths0 = _finder.FindAllPaths(graph, 1, 0).ToList();
            Assert.AreEqual(paths0.Count, 0);

            var paths1 = _finder.FindAllPaths(graph, 1, 1).ToList().ConvertAll(x=>x.Last());
            CollectionAssert.AreEquivalent(new List<int>(){ 2 }, paths1);

            var paths2 = _finder.FindAllPaths(graph, 1, 2).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3,4 }, paths2);

            var paths3 = _finder.FindAllPaths(graph, 1, 3).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4, 5, 6 }, paths3);

            var paths4 = _finder.FindAllPaths(graph, 1, 4).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2, 5, 6 }, paths4);
        }

        [TestMethod]
        public void Looped()
        {
            // 1-2-4
            //   | |
            //   3-5-6
            //     |
            //     7
            var sta1 = new Sta(1, 1);
            var sta2 = new Sta(2, 0);
            var sta3 = new Sta(3, 0);
            var sta4 = new Sta(4, 0);
            var sta5 = new Sta(5, 0);
            var sta6 = new Sta(6, 0);
            var sta7 = new Sta(7, 0);
            var playerPos = new Dictionary<int, int>()
            {
                { 1, 1 }
            };
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6, sta7
            }, playerPos);
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta2.TwowayConnect(sta4);
            sta3.TwowayConnect(sta5);
            sta4.TwowayConnect(sta5);
            sta5.TwowayConnect(sta6);
            sta5.TwowayConnect(sta7);

            var paths2 = _finder.FindAllPaths(graph, 1, 2).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4 }, paths2);

            var paths3 = _finder.FindAllPaths(graph, 1, 3).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 5 }, paths3);

            var paths4 = _finder.FindAllPaths(graph, 1, 4).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4, 6, 7 }, paths4);

            var paths5 = _finder.FindAllPaths(graph, 1, 5).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2 }, paths5);

            var paths6 = _finder.FindAllPaths(graph, 1, 6).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 1 }, paths6);
        }

        [TestMethod]
        public void TransferRestriction()
        {
            //    11  12 [line2]
            //     \ /
            //      1
            //     / \
            //    2   3
            //  ↓ |   |
            //  4-5-6-7-8 [line3]
            //    |   |
            //    9   10 [line1]

            #region buildGraph
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            Sta sta7 = new(7, 1);
            Sta sta8 = new(8, 1);
            Sta sta9 = new(9, 1);
            Sta sta10 = new(10, 1);
            Sta sta11 = new(11, 1);
            Sta sta12 = new(12, 1);
            List<Sta> stas = new() { sta1,sta2,sta3, sta4, sta5, sta6, sta7, sta8, sta9, sta10, sta11, sta12 };
            Graph g = new(stas);
            g.UserPosition.Add(1,4);
            sta11.TwowayConnect(sta1, 1);
            sta1.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta7, 1);
            sta7.TwowayConnect(sta10, 1);

            sta12.TwowayConnect(sta1, 2);
            sta1.TwowayConnect(sta2, 2);
            sta2.TwowayConnect(sta5, 2);
            sta5.TwowayConnect(sta9, 2);

            sta4.TwowayConnect(sta5, 3);
            sta5.TwowayConnect(sta6, 3);
            sta6.TwowayConnect(sta7, 3);
            sta7.TwowayConnect(sta8, 3);
            #endregion

            var paths1 = _finder.FindAllPaths(g, 1, 4, 0).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 8 }, paths1);

            var paths2 = _finder.FindAllPaths(g, 1, 4, 1).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 8, 12, 3, 10 }, paths2);

            var paths3 = _finder.FindAllPaths(g, 1, 4, 2).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 8, 12, 3, 10, 11 }, paths3);
        }

        [TestMethod]
        public void OverlappedLines()
        {
            // 1
            //  \
            // 2-3-7-4-5
            //        \
            //         6
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            Sta sta7 = new(7, 1);
            List<Sta> stas = new() { sta1, sta2, sta3, sta4, sta5, sta6, sta7 };
            Dictionary<int, int> userPosition = new() { { 1, 1 } };
            Graph g = new(stas, userPosition);
            sta1.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta7, 1);
            sta7.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta6, 1);
            sta2.TwowayConnect(sta3, 2);
            sta3.TwowayConnect(sta7, 2);
            sta7.TwowayConnect(sta4, 2);
            sta4.TwowayConnect(sta5, 2);

            var paths_2_0 = _finder.FindAllPaths(g, 1, 2, 0).ToList().ConvertAll(x=>x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 7 }, paths_2_0);

            var paths_2_1 = _finder.FindAllPaths(g, 1, 2, 1).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 7, 2 }, paths_2_1);

            var paths_4_0 = _finder.FindAllPaths(g, 1, 4, 0).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 6 }, paths_4_0);

            var paths_4_1 = _finder.FindAllPaths(g, 1, 4, 1).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 6, 5 }, paths_4_1);
        }

        [TestMethod]
        public void Chicago()
        {
            //     7
            //     |
            //     3
            //    /|\
            // 1-2 | 4-6
            //    \|/
            //     5
            //     |
            //     8

            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            Sta sta7 = new(7, 1);
            Sta sta8 = new(8, 1);
            List<Sta> stas = new() { sta1, sta2, sta3, sta4, sta5, sta6, sta7, sta8 };
            Dictionary<int, int> userPosition = new() { { 1, 1 } };
            Graph g = new(stas, userPosition);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 1);
            sta5.TwowayConnect(sta2, 1);

            sta6.TwowayConnect(sta4, 2);
            sta4.TwowayConnect(sta5, 2);
            sta5.TwowayConnect(sta2, 2);
            sta2.TwowayConnect(sta3, 2);
            sta3.TwowayConnect(sta4, 2);

            sta7.TwowayConnect(sta3, 3);
            sta3.TwowayConnect(sta5, 3);
            sta5.TwowayConnect(sta8, 3);

            var paths_4_0 = _finder.FindAllPaths(g, 1, 4, 0).Select(x=>x.Last()).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 5, 3 }, paths_4_0);

            var paths_4_1 = _finder.FindAllPaths(g, 1, 4, 1).Select(x => x.Last()).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 5, 3, 6, 7, 8 }, paths_4_1);

            var paths_4_2 = _finder.FindAllPaths(g, 1, 4, 2).Select(x => x.Last()).ToList();
            CollectionAssert.AreEquivalent(new List<int>() { 5, 3, 6, 7, 8, 2, 4 }, paths_4_2);
        }

        [TestMethod]
        public void NegativeOne()
        {
            //当步数为-1时，可以一步走到任何其他玩家没占的地方
            //
            //   3
            //   |\
            // 1-2-4-5
            //     |
            //     6
            var sta1 = new Sta(1, 1);
            var sta2 = new Sta(2, 1);
            var sta3 = new Sta(3, 1);
            var sta4 = new Sta(4, 2);
            var sta5 = new Sta(5, 1);
            var sta6 = new Sta(6, 1);
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta3.TwowayConnect(sta4);
            sta2.TwowayConnect(sta4);
            sta4.TwowayConnect(sta5);
            sta4.TwowayConnect(sta6);

            var playerPos = new Dictionary<int, int>() { { 1, 1 } };
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6
            }, playerPos);
            var res = _finder.FindAllPaths(graph, 1, -1).ToList();
            res.ForEach(p => Assert.AreEqual(2, p.Count()));//路径长度为2
            var paths = res.Select(x => x.Last()).ToList();
            CollectionAssert.AreEquivalent(
                new List<int> { 2, 3, 5, 6 }, paths);
        }
    }
}