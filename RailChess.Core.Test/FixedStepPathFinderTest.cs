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

            //测试中关掉超时机制，以便断点调试
            FixedStepPathFinder.DisableTimeoutTestOnly = true;
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
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4 }, paths2);

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
            CollectionAssert.AreEquivalent(new List<int>() { 1, 3, 4 }, paths6);
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
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5, sta6, sta7, sta8, sta9, sta10, sta11, sta12];
            Dictionary<int, List<int>> lines = new()
            {
                { 1, [11, 1, 3, 7, 10] },
                { 2, [12, 1, 2, 5, 9] },
                { 3, [4, 5, 6, 7, 8] }
            };
            Graph g = new(stas, lines);
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
        public void TransferRestriction2()
        {
            //     3
            //    /|
            //   2-4-8
            //   | |
            //   1 5
            //     |
            //     6
            //     |
            //     7

            #region buildGraph
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            Sta sta7 = new(7, 1);
            Sta sta8 = new(8, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5, sta6, sta7, sta8];
            Dictionary<int, List<int>> lines = new()
            {
                { 1, [1, 2, 3] },
                { 2, [2, 4, 8] },
                { 3, [3, 4, 5, 6, 7] }
            };
            Graph g = new(stas, lines);
            g.UserPosition.Add(1, 2);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta2.TwowayConnect(sta4, 2);
            sta4.TwowayConnect(sta8, 2);
            sta3.TwowayConnect(sta4, 3);
            sta4.TwowayConnect(sta5, 3);
            sta5.TwowayConnect(sta6, 3);
            sta6.TwowayConnect(sta7, 3);
            #endregion

            var paths1 = _finder.FindAllPaths(g, 1, 4, 1).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int> { 6, 7 }, paths1);
        }
        
        [TestMethod]
        public void TransferOnRingTail()
        {
            // 1为环线终点，在不允许换乘的情况下
            // 应该可以两步从6走到2
            
            // 1--2--3
            // |     |
            // 6--5--4
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 1);
            sta5.TwowayConnect(sta6, 1);
            sta6.TwowayConnect(sta1, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5, sta6];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 4, 5, 6, 1] }
            });
            g.UserPosition.Add(1, 6);
            int stepCount = 2;
            int maxTransfer = 0;
            var valid = _finder.IsValidMove(g, 1, 2, stepCount, maxTransfer);
            Assert.IsTrue(valid);
        }

        [TestMethod]
        public void TransferOnSelfIntersectT()
        {
            // 线路为123452，不允许换乘的情况下，两步：
            // 可以：从1到3（可以在2两侧找到1、3）
            // 不可以：从1到5（无法在2两侧找到1、5）
            
            // 1--2--3
            //    |  |
            //    5--4
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 1);
            sta5.TwowayConnect(sta2, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 4, 5, 2] }
            });
            g.UserPosition.Add(1, 1);
            int stepCount = 2;
            Assert.IsTrue(_finder.IsValidMove(g, 1, 3, stepCount, 0));
            Assert.IsFalse(_finder.IsValidMove(g, 1, 5, stepCount, 0));
            Assert.IsTrue(_finder.IsValidMove(g, 1, 5, stepCount, 1));
        }

        [TestMethod]
        public void TransferOnSelfIntersectCross()
        {
            // 线路为1234526，不允许换乘的情况下
            // 两步可以：从1到3、从5到6
            // 两步不可以：从1到5、从1到6
            // 三步可以：从3到2
            // 三步不可以：从3到4

            //    6
            //    |
            // 1--2--3
            //    |  |
            //    5--4
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 1);
            sta5.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta6, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5, sta6];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 4, 5, 2, 6] }
            });
            int uid = 1;
            g.UserPosition.Add(uid, 5);
            int stepCount = 2;
            int maxTransfer = 0;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 6, stepCount, maxTransfer)); //5-6可以
            g.UserPosition[uid] = 1;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 3, stepCount, maxTransfer)); //1-3可以
            Assert.IsFalse(_finder.IsValidMove(g, uid, 5, stepCount, maxTransfer)); //1-5不行
            Assert.IsFalse(_finder.IsValidMove(g, uid, 6, stepCount, maxTransfer)); //1-6不行
            Assert.IsTrue(_finder.IsValidMove(g, uid, 5, stepCount, 1)); //但是改成允许换乘又可以了
            Assert.IsTrue(_finder.IsValidMove(g, uid, 6, stepCount, 1)); //但是改成允许换乘又可以了
            stepCount = 3;
            g.UserPosition[uid] = 3;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 2, stepCount, maxTransfer)); //3-2可以
            Assert.IsFalse(_finder.IsValidMove(g, uid, 4, stepCount, maxTransfer)); //3-4不行
            Assert.IsTrue(_finder.IsValidMove(g, uid, 4, stepCount, 1)); //但是改成允许换乘又可以了
        }

        [TestMethod]
        public void TransferOnSelfIntersectY()
        {
            //线路为12324，不允许换乘的情况下
            //两步可从1到3，但不能从1到4

            // 1--2--3
            //   /
            //  4
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta2.TwowayConnect(sta4, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 2, 4] }
            });
            int uid = 1;
            g.UserPosition.Add(uid, 1);
            int stepCount = 2;
            int maxTransfer = 0;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 3, stepCount, maxTransfer));
            Assert.IsFalse(_finder.IsValidMove(g, uid, 4, stepCount, maxTransfer));
            Assert.IsTrue(_finder.IsValidMove(g, uid, 4, stepCount, 1));
        }

        [TestMethod]
        public void TransferOnSelfIntersectYAfterRealTransfer()
        {
            //线路【1】为12324，允许一次换乘的情况下
            //三步可从9到3，但不能从9到4
            //线路【2】为819

            //   9
            //   |
            //8--1--2--3
            //     /
            //    4
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta8 = new(8, 1);
            Sta sta9 = new(9, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta2.TwowayConnect(sta4, 1);
            sta8.TwowayConnect(sta1, 2);
            sta1.TwowayConnect(sta9, 2);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta8, sta9];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 2, 4] },
                { 2, [8, 1, 9] }
            });
            int uid = 1;
            g.UserPosition.Add(uid, 9);
            int stepCount = 3;
            int maxTransfer = 1;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 3, stepCount, maxTransfer));
            Assert.IsFalse(_finder.IsValidMove(g, uid, 4, stepCount, maxTransfer));
            Assert.IsTrue(_finder.IsValidMove(g, uid, 4, stepCount, 2)); //允许两次换乘就能过
        }

        [TestMethod]
        public void TransferOnSelfIntersectOverlappedRange()
        {
            // 线路为12345236，不允许换乘的情况下
            // 3步可以：从1到4、从5到6
            // 3步不可以：从1到6、从5到4（改为允许1次换乘时可以）

            // 1--2--3--6
            //    |  |
            //    5--4

            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 1);
            sta5.TwowayConnect(sta2, 1);
            sta3.TwowayConnect(sta6, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5, sta6];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 4, 5, 2, 3, 6] }
            });
            int uid = 1;
            g.UserPosition[uid] = 1;
            int stepCount = 3; //三步
            Assert.IsTrue(_finder.IsValidMove(g, uid, 4, stepCount, 0)); //1-4可以
            Assert.IsFalse(_finder.IsValidMove(g, uid, 6, stepCount, 0)); //1-6不行
            Assert.IsTrue(_finder.IsValidMove(g, uid, 6, stepCount, 1)); //1-6可以（换乘一次的话）
            Assert.IsTrue(_finder.IsValidMove(g, uid, 6, steps: 7, 0)); //走7步的话，1-6可以无需换乘
            g.UserPosition[uid] = 5;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 6, stepCount, 0)); //5-6可以
            Assert.IsFalse(_finder.IsValidMove(g, uid, 4, stepCount, 0)); //5-4不行
            Assert.IsTrue(_finder.IsValidMove(g, uid, 4, stepCount, 1)); //5-4可以（换乘一次的话）
        }

        [TestMethod]
        public void TransferOnSelfIntersectOverlappedRangeLonger()
        {
            // 线路为1234562347，不允许换乘的情况下
            // 四步可以：从1到5、从6到7
            // 四步不可以：从1到7、从6到5（改为允许1次换乘时可以）

            // 1--2--3--4--7
            //    |     |
            //    6-----5

            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            Sta sta6 = new(6, 1);
            Sta sta7 = new(7, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 1);
            sta5.TwowayConnect(sta6, 1);
            sta6.TwowayConnect(sta2, 1);
            sta4.TwowayConnect(sta7, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5, sta6, sta7];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 4, 5, 6, 2, 3, 4, 7] }
            });
            int uid = 1;
            g.UserPosition[uid] = 1;
            int stepCount = 4; //四步
            Assert.IsTrue(_finder.IsValidMove(g, uid, 5, stepCount, 0)); //1-5可以
            Assert.IsFalse(_finder.IsValidMove(g, uid, 7, stepCount, 0)); //1-7不行
            Assert.IsTrue(_finder.IsValidMove(g, uid, 7, stepCount, 1)); //1-7可以（换乘一次的话）
            Assert.IsTrue(_finder.IsValidMove(g, uid, 7, steps: 9, 0)); //走9步的话，1-7可以无需换乘
            g.UserPosition[uid] = 6;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 7, stepCount, 0)); //6-7可以
            Assert.IsFalse(_finder.IsValidMove(g, uid, 5, stepCount, 0)); //6-5不行
            Assert.IsTrue(_finder.IsValidMove(g, uid, 5, stepCount, 1)); //6-5可以（换乘一次的话）
        }

        [TestMethod]
        public void TransferOnRingTailCross()
        {
            //线路为1231451，在不允许换乘的情况下：
            //两步可以：从3到4、从2到5
            //两步不可以：从3到5、从2到4、从2到3、从4到5

            // 2   5
            // |\ /|
            // | 1 |
            // |/ \|
            // 3   4
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta1, 1);
            sta1.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 1);
            sta5.TwowayConnect(sta1, 1);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 1, 4, 5, 1] }
            });
            int uid = 1;
            g.UserPosition.Add(uid, 1);
            int stepCount = 2;
            int maxTransfer = 0;
            g.UserPosition[uid] = 3;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 4, stepCount, maxTransfer)); //可以3-4
            Assert.IsFalse(_finder.IsValidMove(g, uid, 5, stepCount, maxTransfer)); //不能3-5
            Assert.IsTrue(_finder.IsValidMove(g, uid, 5, stepCount, 1)); //可换乘后又可以了
            g.UserPosition[uid] = 2;
            Assert.IsTrue(_finder.IsValidMove(g, uid, 5, stepCount, maxTransfer)); //可以2-5
            Assert.IsFalse(_finder.IsValidMove(g, uid, 4, stepCount, maxTransfer)); //不能2-4
            Assert.IsFalse(_finder.IsValidMove(g, uid, 3, stepCount, maxTransfer)); //不能2-3
            Assert.IsTrue(_finder.IsValidMove(g, uid, 4, stepCount, 1)); //可换乘后又可以了
            Assert.IsTrue(_finder.IsValidMove(g, uid, 3, stepCount, 1)); //可换乘后又可以了
            g.UserPosition[uid] = 4;
            Assert.IsFalse(_finder.IsValidMove(g, uid, 5, stepCount, maxTransfer)); //不能4-5
            Assert.IsTrue(_finder.IsValidMove(g, uid, 5, stepCount, 1)); //可换乘后又可以了
        }

        [TestMethod]
        public void TransferOnUTip()
        {
            // 1-2-3-4-5
            // ------|--
            // ------|
            Sta sta1 = new(1, 1);
            Sta sta2 = new(2, 1);
            Sta sta3 = new(3, 1);
            Sta sta4 = new(4, 1);
            Sta sta5 = new(5, 1);
            sta1.TwowayConnect(sta2, 1);
            sta2.TwowayConnect(sta3, 1);
            sta3.TwowayConnect(sta4, 1);
            sta4.TwowayConnect(sta5, 2);
            List<Sta> stas = [sta1, sta2, sta3, sta4, sta5];
            Graph g = new(stas, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 4, 3, 2, 1] },
                { 2, [4, 5] }
            });
            int uid = 1;
            g.UserPosition.Add(uid, 1);
            int stepCount = 4;
            int to = 5;
            Assert.IsFalse(_finder.IsValidMove(g, uid, to, stepCount, maxiumTransfer: 0));
            Assert.IsTrue(_finder.IsValidMove(g, uid, to, stepCount, maxiumTransfer: 1));
            Assert.IsTrue(_finder.IsValidMove(g, uid, to, stepCount, maxiumTransfer: 2));
            Assert.IsTrue(_finder.IsValidMove(g, uid, to, stepCount, maxiumTransfer: 3));
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
            Graph g = new(stas, userPosition, new Dictionary<int, List<int>>()
            {
                { 1, [1, 2, 3, 4, 5, 2, 1] },
                { 2, [6, 4, 5, 2, 3, 4, 6] },
                { 3, [7, 3, 5, 8] }
            });
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

        [TestMethod]
        public void AB()
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

            var paths100 = _finder.FindAllPaths(graph, 1, 100).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2 }, paths100);

            var paths101 = _finder.FindAllPaths(graph, 1, 101).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2, 3, 4 }, paths101);

            var paths102 = _finder.FindAllPaths(graph, 1, 102).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4, 5, 6 }, paths102);

            var paths103 = _finder.FindAllPaths(graph, 1, 103).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2, 3, 4, 5, 6 }, paths103);

            var paths200 = _finder.FindAllPaths(graph, 1, 200).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4 }, paths200);

            var paths201 = _finder.FindAllPaths(graph, 1, 201).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2, 3, 4, 5, 6 }, paths201);

            var paths202 = _finder.FindAllPaths(graph, 1, 202).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2, 3, 4, 5, 6 }, paths202);

            var paths300 = _finder.FindAllPaths(graph, 1, 300).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 3, 4, 5, 6 }, paths300);

            var paths301 = _finder.FindAllPaths(graph, 1, 301).ToList().ConvertAll(x => x.Last());
            CollectionAssert.AreEquivalent(new List<int>() { 2, 5, 6 }, paths301);
        }
    }
}