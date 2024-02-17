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
            var sta1 = new Sta(1,1);
            var sta2 = new Sta(2,1);
            var sta3 = new Sta(3,1);
            var sta4 = new Sta(4,1);
            var sta5 = new Sta(5,1);
            var sta6 = new Sta(6,1);
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6
            });
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta3.TwowayConnect(sta4);
            sta2.TwowayConnect(sta4);
            sta4.TwowayConnect(sta5);
            sta4.TwowayConnect(sta6);

            var paths0 = _finder.FindAllPaths(graph, 1, 1, 0).ToList();
            Assert.AreEqual(paths0.Count, 0);

            var paths1 = _finder.FindAllPaths(graph, 1, 1, 1).ToList().ConvertAll(x=>x.Last());
            CollectionAssert.AreEquivalent(new List<int>(){ 2 }, paths1);

            var paths2 = _finder.FindAllPaths(graph, 1, 1, 2).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3,4 }, paths2);

            var paths3 = _finder.FindAllPaths(graph, 1, 1, 3).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4, 5, 6 }, paths3);

            var paths4 = _finder.FindAllPaths(graph, 1, 1, 4).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2, 5, 6 }, paths4);
        }

        [TestMethod]
        public void Combined()
        {
            // 1-2-4
            //   | |
            //   3-5-6
            //     |
            //     7
            var sta1 = new Sta(1, 1);
            var sta2 = new Sta(2, 1);
            var sta3 = new Sta(3, 1);
            var sta4 = new Sta(4, 1);
            var sta5 = new Sta(5, 1);
            var sta6 = new Sta(6, 1);
            var sta7 = new Sta(7, 1);
            Graph graph = new(new(){
                sta1, sta2, sta3, sta4, sta5, sta6, sta7
            });
            sta1.TwowayConnect(sta2);
            sta2.TwowayConnect(sta3);
            sta2.TwowayConnect(sta4);
            sta3.TwowayConnect(sta5);
            sta4.TwowayConnect(sta5);
            sta5.TwowayConnect(sta6);
            sta5.TwowayConnect(sta7);

            var paths2 = _finder.FindAllPaths(graph, 1, 1, 2).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4 }, paths2);

            var paths3 = _finder.FindAllPaths(graph, 1, 1, 3).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 5 }, paths3);

            var paths4 = _finder.FindAllPaths(graph, 1, 1, 4).ToList().ConvertAll(x => x.Last());
            Assert.AreEqual(3, paths4.Count);
            CollectionAssert.IsSubsetOf(new List<int>() { 6, 7 }, paths4);
            CollectionAssert.IsSubsetOf(paths4, new List<int>() { 3,4,6,7 });
        }
    }
}