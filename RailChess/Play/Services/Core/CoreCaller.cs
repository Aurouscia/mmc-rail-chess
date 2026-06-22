using RailChess.Core.Abstractions;

namespace RailChess.Play.Services.Core
{
    public class CoreCaller
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayPlayerService _playerService;
        private readonly PlayGameService _gameService;
        private readonly CoreGraphProvider _graphProvider;
        private readonly IFixedStepPathFinder _fixedStepPathFinder;
        private readonly IExclusiveStasFinder _exclusiveStasFinder;

        public CoreCaller(
            CoreGraphProvider graphProvider,
            IFixedStepPathFinder fixedStepPathFinder,
            IExclusiveStasFinder exclusiveStasFinder,
            PlayEventsService eventsService,
            PlayPlayerService playerService,
            PlayGameService gameService) 
        {
            _graphProvider = graphProvider;
            _fixedStepPathFinder = fixedStepPathFinder;
            _exclusiveStasFinder = exclusiveStasFinder;
            _eventsService = eventsService;
            _playerService = playerService;
            _gameService = gameService;
        }
        /// <summary>
        /// 轮到某玩家时，根据随机出的步数数字，提供可选的路线
        /// </summary>
        /// <returns>可选路线</returns>
        public List<List<int>> GetSelections()
        {
            var graph = _graphProvider.GetGraph();
            var currentUser = _playerService.CurrentPlayer();
            var game = _gameService.OurGame();

            if (game.RandAlg == Models.Game.RandAlgType.FreeWithinRange)
            {
                var steps = Enumerable.Range(game.RandMin, game.RandMax - game.RandMin + 1).ToList();
                var options = new PathFindOptions { Steps = steps, MaxiumTransfer = game.AllowTransfer, AllowReverseAtTerminal = game.AllowReverseAtTerminal };
                var allPaths = _fixedStepPathFinder.FindAllPaths(graph, currentUser, options);
                return allPaths;
            }
            else
            {
                var randNum = _eventsService.RandedResult();
                var options = new PathFindOptions { Steps = [randNum], MaxiumTransfer = game.AllowTransfer, AllowReverseAtTerminal = game.AllowReverseAtTerminal };
                var allPaths = _fixedStepPathFinder.FindAllPaths(graph, currentUser, options);
                return allPaths;
            }
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
            var game = _gameService.OurGame();
            var locationEvents = _eventsService.PlayerLocateEvents();
            var locationEvent = locationEvents.Where(x => x.PlayerId == currentUser).LastOrDefault();
            if (locationEvent is null) throw new Exception("找不到玩家位置(未加入)");
            int location = locationEvent.StationId;

            if (game.RandAlg == Models.Game.RandAlgType.FreeWithinRange)
            {
                var steps = Enumerable.Range(game.RandMin, game.RandMax - game.RandMin + 1).ToList();
                var options = new PathFindOptions { Steps = steps, MaxiumTransfer = game.AllowTransfer };
                return _fixedStepPathFinder.IsValidMove(graph, currentUser, selected, options);
            }
            else
            {
                var randNum = _eventsService.RandedResult();
                var options = new PathFindOptions { Steps = [randNum], MaxiumTransfer = game.AllowTransfer };
                return _fixedStepPathFinder.IsValidMove(graph, currentUser, selected, options);
            }
        }

        /// <summary>
        /// 玩家做出选择且已实际移动后，给出其他玩家再也无法占领的站
        /// </summary>
        /// <returns>已自动占领站id</returns>
        public List<int> AutoCapturables()
        {
            var graph = _graphProvider.GetGraph();
            var lastMovedUser = _eventsService.UserId;
            var options = new ExclusiveStasOptions { Teams = null }; // TODO: 从 game 获取队伍信息
            return _exclusiveStasFinder.FindExclusiveStas(graph, lastMovedUser, options);
        }
    }
}
