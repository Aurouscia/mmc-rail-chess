using RailChess.Models;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Play.PlayHubResponseModel;
using RailChess.Utils;

namespace RailChess.Play.Services
{
    public class PlayResultService
    {
        private readonly RailChessContext _context;
        private readonly PlayPlayerService _playerService;

        public PlayResultService(
            RailChessContext context,
            PlayPlayerService playerService)
        {
            _context = context;
            _playerService = playerService;
        }

        public void RunFor(int gameId, List<Player> playersStatus)
        {
            if (playersStatus.Count <= 1) return;
            var g = _context.Games.Find(gameId);
            if (g is null)
                throw new Exception("找不到指定id的棋局");
            if (g.Ended == false)
                throw new Exception("该棋局还未结束，不能结算");

            playersStatus.Sort((x, y) => y.Score - x.Score);
            var users = _playerService.Get(playersStatus.ConvertAll(x => x.Id));

            Dictionary<User,GameResult> results = new();
            int rank = 1;
            foreach(var player in playersStatus)
            {
                GameResult result = new()
                {
                    GameId = gameId,
                    UserId = player.Id,
                    Score = player.Score,
                    EloDelta = 0,
                    Rank = rank
                };
                rank += 1;
                var u = users.First(x=>x.Id == player.Id);
                results.Add(u, result);
            }
            for (int i = 1; i < playersStatus.Count; i++)
            {
                var pa = playersStatus[i]; 
                User a = users.First(x=>x.Id == pa.Id);
                GameResult resA = results[a];
                for (int j = 0; j < i; j++)
                {
                    //j<i，b排名肯定排a前面
                    var pb = playersStatus[j];
                    User b = users.First(x => x.Id == pb.Id);
                    GameResult resB = results[b];
                    int delta = EloAlg.Delta(a.Elo, b.Elo, pa.Score, pb.Score);

                    resB.EloDelta -= delta;
                    resA.EloDelta += delta;
                    b.Elo -= delta;
                    a.Elo += delta;
                }
            }
            foreach (var item in results)
            {
                _context.Update(item.Key);
                _context.Add(item.Value);
            }
            _context.SaveChanges();
        }
    }
}
