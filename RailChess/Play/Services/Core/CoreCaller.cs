using RailChess.Core.Abstractions;

namespace RailChess.Play.Services.Core
{
    public class CoreCaller
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayPlayerService _playerService;
        private readonly CoreGraphProvider _graphProvider;
        private readonly IFixedStepPathFinder _fixedStepPathFinder;
        private readonly IExclusiveStasFinder _exclusiveStasFinder;

        public CoreCaller(
            CoreGraphProvider graphProvider,
            IFixedStepPathFinder fixedStepPathFinder,
            IExclusiveStasFinder exclusiveStasFinder,
            PlayEventsService eventsService,
            PlayPlayerService playerService) 
        {
            _graphProvider = graphProvider;
            _fixedStepPathFinder = fixedStepPathFinder;
            _exclusiveStasFinder = exclusiveStasFinder;
            _eventsService = eventsService;
            _playerService = playerService;
        }
        /// <summary>
        /// 轮到某玩家时，根据随机出的步数数字，提供可选的路线
        /// </summary>
        /// <returns>可选路线</returns>
        public IEnumerable<IEnumerable<int>> GetSelections()
        {
            //var players = _playerService.GetOrdered();
            var randNum = _eventsService.RandedResult();
            var graph = _graphProvider.GetGraph();
            var currentUser = _eventsService.UserId;
            var allPaths = _fixedStepPathFinder.FindAllPaths(graph, currentUser, randNum);
            return allPaths;
        }

        /// <summary>
        /// 玩家做出选择后，实际移动前，检查该移动是否符合要求
        /// </summary>
        /// <param name="selected"></param>
        /// <returns>是否合法</returns>
        public bool IsValidMove(int selected)
        {
            var currentUser = _eventsService.UserId;
            var graph = _graphProvider.GetGraph();
            var locationEvents = _eventsService.PlayerLocateEvents();
            var locationEvent = locationEvents.Where(x => x.PlayerId == currentUser).LastOrDefault();
            if (locationEvent is null) throw new Exception("找不到玩家位置(未加入)");
            int location = locationEvent.StationId;
            var randNum = _eventsService.RandedResult();
            return _fixedStepPathFinder.IsValidMove(graph, currentUser, location, selected, randNum);
        }

        /// <summary>
        /// 玩家做出选择且已实际移动后，给出其他玩家再也无法占领的站
        /// </summary>
        /// <returns>已自动占领站id</returns>
        public IEnumerable<int> AutoCapturables()
        {
            var graph = _graphProvider.GetGraph();
            var currentUser = _eventsService.UserId;
            return _exclusiveStasFinder.FindExclusiveStas(graph, currentUser);
        }
    }
}
