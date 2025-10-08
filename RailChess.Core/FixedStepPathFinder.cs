﻿using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core
{
    public class FixedStepPathFinder : IFixedStepPathFinder
    {
        /// <inheritdoc/>
        public IEnumerable<IEnumerable<int>> FindAllPaths(Graph graph, int userId, int steps, int maxiumTransfer = int.MaxValue)
        {
            if (steps < 100)
                return FindAllPaths(graph, userId, steps, 0, maxiumTransfer);
            else
            {
                //十位个位算A
                int stepsA = steps % 100;
                //千位百位算B
                int stepsB = steps / 100;
                return FindAllPaths(graph, userId, stepsA, stepsB, maxiumTransfer);
            }
        }

        /// <summary>
        /// 特别感谢Momochai(SlinkierApple13)对算法优化的指导
        /// </summary>
        private static IEnumerable<IEnumerable<int>> FindAllPaths(
            Graph graph, int userId, int stepsA, int stepsB, int maxiumTransfer = int.MaxValue)
        {
            var limit = DateTime.Now.AddSeconds(3);
            int stepsAB = stepsA + stepsB;
            if (stepsA == 0 && stepsB == 0)
                return new List<List<int>>();

            if (!graph.BuildingCompleted)
                graph.CompleteBuilding();

            if (!graph.UserPosition.TryGetValue(userId, out int from))
                throw new Exception("算路异常:找不到玩家位置");

            if (stepsA == -1 && stepsB == 0)
            {
                //如果步数为-1，可一步走到任何未被其他玩家占领的地方
                var mineOrEmpty = graph.Stations.Where(x =>
                    (x.Owner == 0 || x.Owner == userId) && x.Id != from);
                return mineOrEmpty
                    .Select(x => new List<int> { from, x.Id })
                    .ToList();
            }

            Queue<LinedPath> paths = [];
            var startPoint = graph.Stations.Find(x => x.Id == from) ?? throw new Exception("算路异常:找不到指定起始点");
            //出发：可从该站的任何线路的任何索引出发，全部作为初始路径
            List<LinedStaCollapsed> startStas = [];
            if(graph.LineStaIndexes is { })
            {
                foreach(var lineKv in graph.LineStaIndexes)
                {
                    var lineId = lineKv.Key;
                    var dict = lineKv.Value;
                    if (dict.TryGetValue(startPoint.Id, out var indexes))
                    {
                        var ress = indexes.ConvertAll(x => new LinedStaCollapsed(lineId, startPoint, x));
                        startStas.AddRange(ress);
                    }
                }
            }
            else
            {
                //未提供线路信息（单元测试环境）
                startStas = startPoint.Neighbors.ConvertAll(x => new LinedStaCollapsed(x.LineId, startPoint, 0));
            }
            // 将出发点转换为路径
            var initPaths = startStas.Select(x => new LinedPath(x));
            foreach(var p in initPaths)
                paths.Enqueue(p);

            List<LinedPath>? stepsAArchive = null;
            //如果有两种情况，则存储记录步数为A的路径中间产物
            if (stepsA != stepsAB)
                stepsAArchive = [];

            while (true)
            {
                if (!DisableTimeoutTestOnly)
                {
                    //大抵是开销极低的
                    if (DateTime.Now > limit)
                        throw new Exception("计算超时，请联系管理员");
                }
                var p = paths.Dequeue();
                var tail = p.Tail;
                if (tail is null) continue;
                foreach (var n in tail.Station.Neighbors)
                {
                    if (p.TransferredTimes == maxiumTransfer)
                        if (p.Tail?.LineId != n.LineId)
                            continue; // 已经到了换乘上限，不能还往别的线跑
                    if (n.Station.Owner != 0 && n.Station.Owner != userId)
                        continue; // 不是自己的/空的就不能往这走
                    if (p.Stations.Count >= 2)
                    {
                        var lastButOne = p.Stations[^2];
                        if (lastButOne.Station.Id == n.Station.Id) continue; //不能掉头往回跑
                    }
                    var nCollapseRes = LinedStaCollapsed.Collapse(n);
                    foreach (var ncr in nCollapseRes)
                    {
                        bool needTransfer = false;
                        if(tail.LineId == ncr.LineId)
                        {
                            // 线路一样：判断自交（线上索引是否相邻）
                            if (graph.Lines.Count > 0) {
                                var line = graph.Lines[tail.LineId];
                                needTransfer = !IsSerialNeighborInLine(tail.IndexChosen, ncr.IndexChosen, line);
                            }
                        }
                        else
                        {
                            // 线路不一样：必然是换乘（但不一定合法）
                            needTransfer = true;
                            // 线路不一样：确保新点和其线上的tail点相邻，否则continue
                            if (graph.Lines.Count > 0 && graph.LineStaIndexes is { })
                            {
                                var line = graph.Lines[tail.LineId];
                                var tailOnNLineIndexes = graph.LineStaIndexes[n.LineId][tail.Station.Id];
                                if (tailOnNLineIndexes.All(x => !IsSerialNeighborInLine(x, ncr.IndexChosen, line)))
                                    continue;
                            }
                        }
                        if (needTransfer)
                        {
                            if (p.TransferredTimes + 1 > maxiumTransfer)
                                continue; // 不准换乘次数超出限制
                        }
                        LinedPath newPath = new(p, ncr, needTransfer);
                        paths.Enqueue(newPath);
                        if (stepsAArchive is { } && newPath.Count == stepsA + 1)
                            stepsAArchive.Add(newPath);
                    }
                }
                if (paths.Count == 0) break;
                if (paths.All(x => x.Count >= stepsAB + 1)) break;
                //由于paths是队列，所以每个潜在路径都是按顺序逐个延长的，满足上一句的条件，应该长度全都一样
            }

            IEnumerable<LinedPath> pathsFinal = paths.AsEnumerable();
            if (stepsAArchive is { })
                pathsFinal = pathsFinal.Concat(stepsAArchive);
            return pathsFinal
                .Where(x => (x.Count == stepsA + 1) || (x.Count == stepsAB + 1))
                .DistinctBy(x => x.Tail?.Station.Id)
                .Select(x => x.ToIds())
                .ToList();
        }

        public bool IsValidMove(Graph graph, int userId, int to, int steps, int maxiumTransfer = int.MaxValue)
        {
            var paths = FindAllPaths(graph, userId, steps, maxiumTransfer);
            return paths.Any(x => x.LastOrDefault() == to);
        }

        public static bool DisableTimeoutTestOnly { get; set; } = false;

        private class LinedPath
        {
            public List<LinedStaCollapsed> Stations { get; }
            public int TransferredTimes { get; private set; }
            public LinedStaCollapsed? Tail => Stations.LastOrDefault();
            public int Count => Stations.Count;
            public LinedPath(LinedStaCollapsed head)
            {
                Stations = [head];
            }
            public LinedPath(LinedPath basedOn, LinedStaCollapsed newSta, bool transfer)
            {
                Stations = [.. basedOn.Stations];
                TransferredTimes = basedOn.TransferredTimes;
                Stations.Add(newSta);
                if (transfer)
                    TransferredTimes++;
            }

            public IEnumerable<int> ToIds()
            {
                return Stations.ConvertAll(x => x.Station.Id);
            }
        }

        /// <summary>
        /// 带有所属线路id+在线路中的索引的站点<br/>
        /// <see cref="LinedSta"/>虽然指定了线路，但可能在线路中多次出现<br/>
        /// “确定其在线路中的索引”这个操作称为“坍缩 Collapse”（模仿量子力学的概念）
        /// </summary>
        private class LinedStaCollapsed : LinedSta
        {
            public LinedStaCollapsed(
                LinedSta sta, int indexChosen
                ) : base(sta.LineId, sta.Station)
            {
                IndexChosen = indexChosen;
            }
            public LinedStaCollapsed(
                int lineId, Sta station, int indexChosen
                ) : base(lineId, station)
            {
                IndexChosen = indexChosen;
            }
            /// <summary>
            /// 其在<see cref="LinedSta.LineId"/>中的索引
            /// </summary>
            public int IndexChosen { get; }
            /// <summary>
            /// 将<see cref="LinedSta"/>在其所属线路中的索引的所有可能性全部列出来
            /// </summary>
            /// <param name="sta">目标</param>
            /// <returns>坍缩结果</returns>
            public static List<LinedStaCollapsed> Collapse(LinedSta sta)
            {
                return sta.Indexes is { }
                    ? sta.Indexes.ConvertAll(x => new LinedStaCollapsed(sta, x)) ?? []
                    : [new(sta, 0)]; // 无线路信息（单元测试环境）
            }
        }

        /// <summary>
        /// 判断两个索引在线路中是否相邻<br/>
        /// 环线：长度至少3且首尾id相同
        /// </summary>
        /// <param name="indexA">索引a</param>
        /// <param name="indexB">索引b</param>
        /// <param name="line">线路</param>
        /// <returns>是否相邻</returns>
        private static bool IsSerialNeighborInLine(int indexA, int indexB, List<int> line)
        {
            var isRing = line.Count >= 3 && line.First() == line.Last();
            var diff = Math.Abs(indexA - indexB);
            if (diff == 1)
                return true;
            if (isRing)
                return diff == line.Count - 2;
            return false;
        }
    }
}