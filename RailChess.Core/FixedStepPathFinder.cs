using System.Diagnostics;
using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core
{
    public class FixedStepPathFinder : IFixedStepPathFinder
    {
        public List<List<int>> FindAllPaths(Graph graph, int userId, PathFindOptions options)
        {
            var steps = options.Steps;
            if (steps is null || steps.Count == 0)
                return new List<List<int>>();

            // 先解析每个步数：>=100 的拆分为两个，其他的保持原样
            List<int> expandedSteps = [];
            foreach (var s in steps)
            {
                if (s >= 100)
                {
                    int stepsA = s % 100;
                    int stepsB = s / 100;
                    expandedSteps.Add(stepsA);
                    expandedSteps.Add(stepsA + stepsB);
                }
                else
                {
                    expandedSteps.Add(s);
                }
            }

            // 去重并排序（从小到大，保证BFS可以复用中间结果）
            var uniqueSteps = expandedSteps.Distinct().OrderBy(x => x).ToList();
            if (uniqueSteps.Count == 0)
                return new List<List<int>>();

            return FindAllPathsCore(graph, userId, uniqueSteps, options);
        }

        /// <summary>
        /// 特别感谢Momochai(SlinkierApple13)对算法优化的指导
        /// </summary>
        private List<List<int>> FindAllPathsCore(
            Graph graph, int userId, List<int> stepsList, PathFindOptions options)
        {
            var maxiumTransfer = options.MaxiumTransfer;
            var allowReverseAtTerminal = options.AllowReverseAtTerminal;
            var teammates = GetTeammates(options.Teams, userId);
            var limit = DateTime.Now.AddSeconds(3);
            ResetNeighborsTriedTimesTestOnly();

            int maxSteps = stepsList.Max();
            if (maxSteps == 0 && !stepsList.Contains(-1))
                return new List<List<int>>();

            if (!graph.BuildingCompleted)
                graph.CompleteBuilding();

            if (!graph.UserPosition.TryGetValue(userId, out int from))
                throw new Exception("算路异常:找不到玩家位置");

            // 处理 -1 的特殊情况
            var normalSteps = stepsList.Where(s => s != -1).ToList();
            var hasNegativeOne = stepsList.Contains(-1);

            List<List<int>> negativeOnePaths = [];
            if (hasNegativeOne)
            {
                var mineOrTeammate = graph.Stations.Where(x =>
                    (x.Owner == 0 || x.Owner == userId || teammates.Contains(x.Owner)) && x.Id != from);
                negativeOnePaths = mineOrTeammate
                    .Select(x => new List<int> { from, x.Id })
                    .Where(x => !IsTeammateCurrentPosition(graph, teammates, x.Last()))
                    .ToList();
            }

            if (normalSteps.Count == 0)
                return negativeOnePaths;

            // 按步数从小到大排序，用于归档中间结果
            var sortedSteps = normalSteps.OrderBy(x => x).ToList();
            var stepArchives = new Dictionary<int, List<LinedPath>>();
            foreach (var s in sortedSteps)
                stepArchives[s] = [];

            var startPoint = graph.Stations.Find(x => x.Id == from) ?? throw new Exception("算路异常:找不到指定起始点");
            List<LinedStaCollapsed> startStas = [];
            if (graph.LineStaIndexes is not null)
            {
                foreach (var (lineId, dict) in graph.LineStaIndexes)
                {
                    if (dict.TryGetValue(startPoint.Id, out var indexes))
                    {
                        var ress = indexes.ConvertAll(x => new LinedStaCollapsed(lineId, startPoint, x));
                        startStas.AddRange(ress);
                    }
                }
            }
            else
            {
                //未提供线路信息（单元测试环境）统一加到第0位置
                startStas = startPoint.Neighbors.ConvertAll(x => new LinedStaCollapsed(x.LineId, startPoint, 0));
            }

            int minStepRequired = sortedSteps.Min();
            Queue<LinedPath> paths = [];
            var initPaths = startStas
                .Where(x => CanInitialDirectionPossiblyReach(graph, x, userId, minStepRequired, allowReverseAtTerminal, maxiumTransfer, teammates))
                .Select(x => new LinedPath(x));
            foreach (var p in initPaths)
                paths.Enqueue(p);

            // 记录每个步数是否已完结的终点（用于剪枝）
            var confirmedDestByStep = new Dictionary<int, HashSet<int>>();
            foreach (var s in sortedSteps)
                confirmedDestByStep[s] = [];

            while (true)
            {
                if (!DisableTimeoutTestOnly)
                {
                    //大抵是开销极低的
                    if (DateTime.Now > limit)
                        throw new Exception("计算超时，请联系管理员");
                }

                if (paths.Count == 0) break;
                var currentPeek = paths.Peek();
                if (currentPeek.Count > maxSteps) break;

                var p = paths.Dequeue();
                if (p.Redundant)
                    continue; // 跳过被标记为抛弃的路线
                var pTail = p.Tail;
                if (pTail is null)
                    continue;

                int currentStepCount = p.Count - 1; // 已走步数
                bool pJustStared = p.Count == 1;
                bool transferUsedUp = p.TransferredTimes == maxiumTransfer;

                // 判断当前路径是否接近某个目标步数（还差一步就达到目标）
                var nearFullSteps = sortedSteps.Where(s => currentStepCount == s - 1).ToList();

                // 邻点必须全部考虑，否则会漏掉“换乘到并行线后再分叉”的情况
                // 见测试 TransferThenSplit 方法
                var neighbors = pTail.Station.Neighbors;
                HashSet<int> reachableBySameLine = [];
                foreach (var n in neighbors)
                {
                    if (n.LineId == pTail.LineId)
                        reachableBySameLine.Add(n.Station.Id);
                }
                foreach (var n in neighbors)
                {
                    bool isTransfer = pTail.LineId != n.LineId;
                    if (isTransfer && reachableBySameLine.Contains(n.Station.Id))
                        continue; // 本来可以同线路到达一样的站，就不要换乘了（确保共线段仅在进入之初或离开时换乘）

                    // 剪枝策略说明（参见单元测试 MultiStepOverPruningBug）：
                    // 当存在多个目标步数（如 [5,6]）时，若对每一个目标步数都因为"已有其他路径到达该终点"而跳过入队，
                    // 则较短步数（如 5）确认过的路径会被丢弃，无法继续向更长步数（如 6）扩展，导致漏掉可达站点。
                    // 因此仅当该目标步数等于最大请求步数 maxSteps 时才跳过；到达最大步数后不会再继续扩展，剪枝才是安全的。
                    bool shouldSkipByConfirmed = false;
                    foreach (var ns in nearFullSteps)
                    {
                        if (confirmedDestByStep[ns].Contains(n.Station.Id) && ns == maxSteps)
                        {
                            shouldSkipByConfirmed = true;
                            break;
                        }
                    }
                    if (shouldSkipByConfirmed)
                        continue;

                    if (transferUsedUp || pJustStared)
                        if (isTransfer)
                            continue; // 已经到了换乘上限，不能往别的线跑；走出的第一步，也不能往别的线跑
                    if (!IsEmptyOrMineOrTeammateOwned(n.Station, userId, teammates))
                        continue; // 不是自己的/空的/队友的就不能往这走
                    if (p.Stations.Count >= 2)
                    {
                        var lastButOne = p.Stations[^2];
                        if (lastButOne.Station.Id == n.Station.Id)
                        {
                            // 默认禁止掉头；若允许在终点折返，则检查当前所在站是否在线路端点
                            if (!allowReverseAtTerminal || !IsAtTerminal(pTail, graph))
                                continue;
                        }
                    }

                    IncrementNeighborsTriedTimesTestOnly();

                    var nCollapseRes = LinedStaCollapsed.Collapse(n);
                    foreach (var ncr in nCollapseRes)
                    {
                        bool needTransfer = false;
                        if (pTail.LineId == ncr.LineId)
                        {
                            // 线路一样：判断自交（线上索引是否相邻）
                            if (graph.Lines.Count > 0)
                            {
                                // 仅在有线路信息时进入判断
                                var line = graph.Lines[pTail.LineId];
                                needTransfer = !IsSerialNeighborInLine(pTail.IndexChosen, ncr.IndexChosen, line);
                            }
                        }
                        else
                        {
                            // 线路不一样：必然是换乘（但不一定能这么走）
                            needTransfer = true;
                            if (transferUsedUp)
                                continue;
                            // 线路不一样：确保新点和其线上的tail点相邻，否则continue
                            if (graph.Lines.Count > 0 && graph.LineStaIndexes is not null)
                            {
                                // 仅在有线路信息时进入判断
                                var line = graph.Lines[pTail.LineId];
                                var tailOnNLineIndexes = graph.LineStaIndexes[n.LineId][pTail.Station.Id];
                                if (tailOnNLineIndexes.All(x => !IsSerialNeighborInLine(x, ncr.IndexChosen, line)))
                                    continue;
                            }
                        }
                        var transferredTimesNew = p.TransferredTimes;
                        if (needTransfer)
                        {
                            transferredTimesNew += 1;
                            if (transferredTimesNew > maxiumTransfer)
                                continue; // 不准换乘次数超出限制
                        }
                        if (graph.Lines.Count > 0)
                        {
                            // （半路上）避免添加多余线路
                            // 如果“新路线”与“已有路线”长度一致，且第n、n-1站一致（站id、线路id、线中索引都一致）
                            // 那么认为“新路线”与“已有路线”冲突，抛弃已换乘次数更多的那个
                            bool newPathRedundant = false;
                            foreach (var ep in paths)
                            {
                                if (ep.Count != p.Count + 1)
                                    continue;
                                bool conflict = false;
                                if (ep.Tail?.IsEquivAs(ncr) ?? false)
                                {
                                    if (ep.Count < 2)
                                        conflict = true; // 唯一站一致（不太可能），冲突
                                    else
                                    {
                                        var epLastButOne = ep.Stations[^2];
                                        if (epLastButOne.IsEquivAs(pTail))
                                            conflict = true; // 第n、n-1站一致，冲突
                                    }
                                }
                                if (conflict)
                                {
                                    if (ep.TransferredTimes > transferredTimesNew)
                                        ep.Redundant = true; // 标记原路线为多余的（抛弃）
                                    else
                                        newPathRedundant = true; // 标记新路线为多余的（不添加）
                                    break;
                                }
                            }
                            if (newPathRedundant)
                                continue; // 新路线多余：不构造，直接去下个循环
                        }
                        LinedPath newPath = new(p, ncr, needTransfer);
                        paths.Enqueue(newPath);

                        int newPathStepCount = newPath.Count - 1;
                        // 归档：如果新路径的步数等于某个目标步数
                        foreach (var targetStep in sortedSteps)
                        {
                            if (newPathStepCount == targetStep)
                            {
                                stepArchives[targetStep].Add(newPath);
                                confirmedDestByStep[targetStep].Add(ncr.Station.Id);
                            }
                        }

                        // 如果新路径达到最大步数，确认终点并跳出（无需再尝试同邻点的其他坍缩情况）
                        if (newPathStepCount == maxSteps)
                        {
                            break;
                        }
                    }
                }
            }

            // 收集结果：从paths队列和归档中收集所有目标步数的路径
            var resultPaths = new List<LinedPath>();
            foreach (var targetStep in sortedSteps)
            {
                // 从队列中收集
                resultPaths.AddRange(paths.Where(x => x.Count == targetStep + 1));
                // 从归档中收集
                resultPaths.AddRange(stepArchives[targetStep]);
            }

            var result = resultPaths
                .DistinctBy(x => x.Tail?.Station.Id)
                .Select(x => x.ToIds().ToList())
                .Where(x => !IsTeammateCurrentPosition(graph, teammates, x.Last()))
                .ToList();

            // 合并 -1 的结果
            if (hasNegativeOne)
            {
                result.AddRange(negativeOnePaths);
            }

            return result;
        }

        public bool IsValidMove(Graph graph, int userId, int to, PathFindOptions options) =>
            FindAllPaths(graph, userId, options).Any(x => x.LastOrDefault() == to);

        public static bool DisableTimeoutTestOnly { get; set; }
        [Conditional("DEBUG")]
        private void ResetNeighborsTriedTimesTestOnly() => NeighborsTriedTimesTestOnly = 0;
        [Conditional("DEBUG")]
        private void IncrementNeighborsTriedTimesTestOnly() => NeighborsTriedTimesTestOnly++;
        public int NeighborsTriedTimesTestOnly { get; private set; }

        private class LinedPath
        {
            public List<LinedStaCollapsed> Stations { get; }
            public int TransferredTimes { get; private set; }
            public LinedStaCollapsed? Tail => Stations.LastOrDefault();
            public int Count => Stations.Count;
            public bool Redundant { get; set; }
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

            public List<int> ToIds() =>
                Stations.ConvertAll(x => x.Station.Id);
        }

        /// <summary>
        /// 带有所属线路id+在线路中的索引的站点<br/>
        /// <see cref="LinedSta"/>虽然指定了线路，但可能在线路中多次出现<br/>
        /// “确定其在线路中的索引”这个操作称为“坍缩 Collapse”（模仿量子力学的概念）
        /// </summary>
        private class LinedStaCollapsed : LinedSta
        {
            private LinedStaCollapsed(
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
            public static List<LinedStaCollapsed> Collapse(LinedSta sta) =>
                sta.Indexes is not null
                    ? sta.Indexes.ConvertAll(x => new LinedStaCollapsed(sta, x))
                    : [new LinedStaCollapsed(sta, 0)]; // 无线路信息（单元测试环境）
            public bool IsEquivAs(LinedStaCollapsed other) =>
                this.LineId == other.LineId
                    && this.Station.Id == other.Station.Id
                    && this.IndexChosen == other.IndexChosen;
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
            var isRing = IsRingLine(line);
            var diff = Math.Abs(indexA - indexB);
            if (diff == 1)
                return true;
            if (isRing)
                return diff == line.Count - 2;
            return false;
        }

        /// <summary>
        /// 判断当前站点是否为其所在线路的终点（端点）<br/>
        /// 线性线路：索引为0或最后一个；环线：无终点
        /// </summary>
        private static bool IsAtTerminal(LinedStaCollapsed sta, Graph graph)
        {
            if (graph.Lines.Count == 0)
                return false; // 无线路信息
            if (!graph.Lines.TryGetValue(sta.LineId, out var line))
                return false;

            // 环线无终点
            if (IsRingLine(line))
                return false;

            // 线性线路的端点
            return sta.IndexChosen == 0 || sta.IndexChosen == line.Count - 1;
        }

        /// <summary>
        /// 线性扫描剪枝：在换乘次数为0的情况下，
        /// 若从指定索引出发沿线路单向（可考虑环线、折返、障碍物）最远可走步数小于 minSteps，
        /// 则该初始方向不可能完成任何目标，可跳过入队。
        /// </summary>
        private static bool CanInitialDirectionPossiblyReach(
            Graph graph, LinedStaCollapsed startSta, int userId, int minSteps,
            bool allowReverseAtTerminal, int maxiumTransfer, HashSet<int> teammates)
        {
            // 仅在换乘次数为0时启用此保守剪枝
            if (maxiumTransfer != 0)
                return true;

            if (minSteps <= 0)
                return true;

            // 无线路信息时无法判断，保守保留
            if (graph.Lines.Count == 0 || graph.LineStaIndexes is null)
                return true;

            if (!graph.Lines.TryGetValue(startSta.LineId, out var line))
                return true;

            int leftSteps = ScanWalkableStepsOnLine(
                graph, line, startSta.IndexChosen, -1,
                allowReverseAtTerminal, minSteps, userId, teammates);
            int rightSteps = ScanWalkableStepsOnLine(
                graph, line, startSta.IndexChosen, +1,
                allowReverseAtTerminal, minSteps, userId, teammates);

            return Math.Max(leftSteps, rightSteps) >= minSteps;
        }

        /// <summary>
        /// 在线路上沿指定初始方向行走，考虑环线、折返规则和站点占领状态，
        /// 返回在不换乘的情况下可连续经过的最大步数。
        /// 扫描步数上限为 maxScanSteps，避免折返或环线导致无限循环。
        /// </summary>
        private static int ScanWalkableStepsOnLine(
            Graph graph, List<int> line, int startIndex, int initialDirection,
            bool allowReverseAtTerminal, int maxScanSteps, int userId, HashSet<int> teammates)
        {
            bool isRing = IsRingLine(line);
            int currentIndex = startIndex;
            int direction = initialDirection;
            int steps = 0;

            while (steps < maxScanSteps)
            {
                int? nextIndex = GetNextIndexOnLine(line, currentIndex, direction, isRing);

                if (nextIndex is null)
                {
                    // 非环线到达物理端点，如果不允许折返则直接结束，允许折返则反向继续
                    if (!allowReverseAtTerminal)
                        break;

                    direction *= -1;
                    continue;
                }

                var station = graph.Stations.Find(s => s.Id == line[nextIndex.Value]);
                if (station is null)
                    break;

                if (!IsEmptyOrMineOrTeammateOwned(station, userId, teammates))
                    break;

                currentIndex = nextIndex.Value;
                steps++;
            }

            return steps;
        }

        /// <summary>
        /// 获取线路上沿指定方向移动的下一个索引。
        /// 环线会跳过首尾重复的端点；非环线到达物理端点时返回 null。
        /// </summary>
        private static int? GetNextIndexOnLine(List<int> line, int currentIndex, int direction, bool isRing)
        {
            int nextIndex = currentIndex + direction;

            if (isRing)
            {
                if (nextIndex >= line.Count)
                    nextIndex = 1; // 跳过与索引0重复的末尾
                else if (nextIndex < 0)
                    nextIndex = line.Count - 2; // 跳过与末尾重复的索引0
            }

            if (nextIndex < 0 || nextIndex >= line.Count)
                return null;

            return nextIndex;
        }

        /// <summary>
        /// 判断线路是否为环线（长度至少3且首尾id相同）
        /// </summary>
        private static bool IsRingLine(List<int> line) =>
            line.Count >= 3 && line.First() == line.Last();

        /// <summary>
        /// 获取指定玩家的队友集合（不含自己）<br/>
        /// 支持一个玩家同时属于多个队伍
        /// </summary>
        private static HashSet<int> GetTeammates(List<List<int>>? teams, int userId)
        {
            if (teams is null)
                return new HashSet<int>();
            return teams
                .Where(t => t.Contains(userId))
                .SelectMany(t => t)
                .Where(id => id != userId)
                .ToHashSet();
        }

        /// <summary>
        /// 判断指定站点是否为空、被自己占领或被队友占领（即可安全经过）
        /// </summary>
        private static bool IsEmptyOrMineOrTeammateOwned(Sta station, int userId, HashSet<int> teammates) =>
            station.Owner == 0 || station.Owner == userId || teammates.Contains(station.Owner);

        /// <summary>
        /// 判断指定站点是否是某个队友的当前位置
        /// </summary>
        private static bool IsTeammateCurrentPosition(Graph graph, HashSet<int> teammates, int stationId)
        {
            foreach (var (playerId, pos) in graph.UserPosition)
            {
                if (teammates.Contains(playerId) && pos == stationId)
                    return true;
            }
            return false;
        }
    }
}
