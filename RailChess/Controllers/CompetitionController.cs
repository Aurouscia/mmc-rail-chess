using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Services;

namespace RailChess.Controllers
{
    public class CompetitionController : Controller
    {
        private readonly RailChessContext _context;
        private readonly int _userId;
        public CompetitionController(
            RailChessContext context,
            HttpUserIdProvider httpUserIdProvider)
        {
            _context = context;
            _userId = httpUserIdProvider.Get();
        }

        /// <summary>创建比赛</summary>
        [Authorize]
        public IActionResult Create([FromBody] CompetitionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return this.ApiFailedResp("比赛标题不能为空");
            if (dto.Title.Length > 128)
                return this.ApiFailedResp("比赛标题过长");

            var competition = dto.ToCompetition();
            competition.HostUserId = _userId;
            competition.CreateTime = DateTime.UtcNow;
            competition.Status = CompetitionStatus.Planned;
            competition.Deleted = false;
            competition.Matches = new List<CompetitionMatch>();

            _context.Competitions.Add(competition);
            _context.SaveChanges();
            return this.ApiResp();
        }

        /// <summary>比赛列表</summary>
        public IActionResult List(int skip = 0, int take = 20)
        {
            take = Math.Clamp(take, 1, 100);
            var query = _context.Competitions
                .Where(x => !x.Deleted)
                .OrderByDescending(x => x.StartTime);

            var total = query.Count();
            var pageItems = query
                .Skip(skip)
                .Take(take)
                .ToList();

            var competitionIds = pageItems.Select(x => x.Id).ToList();
            var hostIds = pageItems.Select(x => x.HostUserId).Distinct().ToList();

            var matchCounts = _context.CompetitionMatches
                .Where(m => competitionIds.Contains(m.CompetitionId))
                .GroupBy(m => m.CompetitionId)
                .ToDictionary(g => g.Key, g => g.Count());

            var hostNames = _context.Users
                .Where(u => hostIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name);

            var items = pageItems
                .Select(x => CompetitionDto.From(
                    x,
                    hostNames,
                    matchCounts.GetValueOrDefault(x.Id),
                    string.IsNullOrWhiteSpace(x.ParticipantUserIdCsv)
                        ? 0
                        : x.ParticipantUserIdCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Length))
                .ToList();

            return this.ApiResp(new { total, items });
        }

        /// <summary>比赛详情（包含对局列表）</summary>
        public IActionResult Detail(int id)
        {
            var competition = _context.Competitions
                .Include(x => x.Matches)
                .ThenInclude(x => x.Game)
                .FirstOrDefault(x => x.Id == id && !x.Deleted);

            if (competition is null)
                return this.ApiFailedResp("找不到指定比赛");

            var gameIds = competition.Matches.Select(m => m.GameId).ToList();
            var gameHosts = _context.Games
                .Where(g => gameIds.Contains(g.Id))
                .Select(g => new { g.Id, g.HostUserId, g.GameName })
                .ToList();

            var hostIds = gameHosts.Select(g => g.HostUserId).Distinct().ToList();
            var hosts = _context.Users
                .Where(u => hostIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToList();

            var detail = CompetitionDto.From(competition);
            detail.Matches = competition.Matches
                .OrderBy(m => m.OrderIndex)
                .ThenBy(m => m.Id)
                .Select(m =>
                {
                    var gh = gameHosts.FirstOrDefault(g => g.Id == m.GameId);
                    var hostName = gh is null ? null : hosts.FirstOrDefault(u => u.Id == gh.HostUserId)?.Name;
                    return new CompetitionMatchDto
                    {
                        MatchId = m.Id,
                        GameId = m.GameId,
                        OrderIndex = m.OrderIndex,
                        Stage = m.Stage,
                        ScheduledStartTime = CompetitionDto.ToTimestamp(m.ScheduledStartTime),
                        GameName = gh?.GameName,
                        HostUserName = hostName
                    };
                })
                .ToList();

            return this.ApiResp(detail);
        }

        /// <summary>更新比赛基本信息</summary>
        [Authorize]
        public IActionResult Update([FromBody] CompetitionDto dto)
        {
            if (dto is null || dto.Id <= 0)
                return BadRequest();

            var existing = _context.Competitions.Find(dto.Id);
            if (existing is null || existing.Deleted)
                return this.ApiFailedResp("找不到指定比赛");
            if (existing.HostUserId != _userId)
                return this.ApiFailedResp("只有创建者能修改比赛");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return this.ApiFailedResp("比赛标题不能为空");
            if (dto.Title.Length > 128)
                return this.ApiFailedResp("比赛标题过长");
            if (dto.Description is not null && dto.Description.Length > 1024)
                return this.ApiFailedResp("比赛说明过长");

            existing.Title = dto.Title;
            existing.Description = dto.Description;
            existing.StartTime = CompetitionDto.FromTimestamp(dto.StartTime);
            existing.EndTime = CompetitionDto.FromTimestamp(dto.EndTime);
            existing.Status = dto.Status;

            _context.SaveChanges();
            return this.ApiResp();
        }

        /// <summary>删除比赛（软删除）</summary>
        [Authorize]
        public IActionResult Delete(int id)
        {
            var competition = _context.Competitions.Find(id);
            if (competition is null || competition.Deleted)
                return this.ApiFailedResp("找不到指定比赛");
            if (competition.HostUserId != _userId)
                return this.ApiFailedResp("只有创建者能删除比赛");

            competition.Deleted = true;
            _context.SaveChanges();
            return this.ApiResp();
        }

        /// <summary>向比赛中添加一局已创建的游戏</summary>
        [Authorize]
        public IActionResult AddMatch(int competitionId, int gameId)
        {
            var competition = _context.Competitions
                .Include(x => x.Matches)
                .FirstOrDefault(x => x.Id == competitionId && !x.Deleted);
            if (competition is null)
                return this.ApiFailedResp("找不到指定比赛");
            if (competition.HostUserId != _userId)
                return this.ApiFailedResp("只有创建者能修改比赛");

            var game = _context.Games.FirstOrDefault(x => x.Id == gameId && !x.Deleted);
            if (game is null)
                return this.ApiFailedResp("找不到指定棋局");

            if (competition.Matches.Any(m => m.GameId == gameId))
                return this.ApiFailedResp("该棋局已在本比赛中");

            var nextOrder = competition.Matches.Count > 0
                ? competition.Matches.Max(m => m.OrderIndex) + 1
                : 0;

            var match = new CompetitionMatch
            {
                CompetitionId = competitionId,
                GameId = gameId,
                OrderIndex = nextOrder
            };
            competition.Matches.Add(match);

            UpdateParticipantCsv(competition);
            _context.SaveChanges();
            return this.ApiResp();
        }

        /// <summary>从比赛中移除一局游戏</summary>
        [Authorize]
        public IActionResult RemoveMatch(int competitionId, int gameId)
        {
            var competition = _context.Competitions
                .Include(x => x.Matches)
                .FirstOrDefault(x => x.Id == competitionId && !x.Deleted);
            if (competition is null)
                return this.ApiFailedResp("找不到指定比赛");
            if (competition.HostUserId != _userId)
                return this.ApiFailedResp("只有创建者能修改比赛");

            var match = competition.Matches.FirstOrDefault(m => m.GameId == gameId);
            if (match is null)
                return this.ApiFailedResp("该棋局不在本比赛中");

            competition.Matches.Remove(match);
            _context.CompetitionMatches.Remove(match);

            UpdateParticipantCsv(competition);
            _context.SaveChanges();
            return this.ApiResp();
        }

        /// <summary>更新比赛中对局的展示顺序</summary>
        [Authorize]
        public IActionResult UpdateMatchOrder(int competitionId, [FromBody] List<int> matchIds)
        {
            var competition = _context.Competitions
                .Include(x => x.Matches)
                .FirstOrDefault(x => x.Id == competitionId && !x.Deleted);
            if (competition is null)
                return this.ApiFailedResp("找不到指定比赛");
            if (competition.HostUserId != _userId)
                return this.ApiFailedResp("只有创建者能修改比赛");

            var matchDict = competition.Matches.ToDictionary(m => m.Id);
            if (matchIds.Count != matchDict.Count || matchIds.Any(id => !matchDict.ContainsKey(id)))
                return this.ApiFailedResp("对局顺序数据异常");

            for (int i = 0; i < matchIds.Count; i++)
            {
                matchDict[matchIds[i]].OrderIndex = i;
            }

            _context.SaveChanges();
            return this.ApiResp();
        }

        /// <summary>更新对局预计开始时间</summary>
        [Authorize]
        public IActionResult UpdateMatchScheduledStartTime(int matchId, long scheduledStartTime)
        {
            var match = _context.CompetitionMatches
                .Include(m => m.Competition)
                .FirstOrDefault(m => m.Id == matchId);
            if (match is null || match.Competition is null || match.Competition.Deleted)
                return this.ApiFailedResp("找不到指定对局");
            if (match.Competition.HostUserId != _userId)
                return this.ApiFailedResp("只有创建者能修改比赛");

            match.ScheduledStartTime = CompetitionDto.FromTimestamp(scheduledStartTime);
            _context.SaveChanges();
            return this.ApiResp();
        }

        /// <summary>根据比赛下所有对局的 AllowUserIdCsv 重新计算参赛用户并集</summary>
        private void UpdateParticipantCsv(Competition competition)
        {
            var gameIds = competition.Matches.Select(m => m.GameId).ToList();
            if (gameIds.Count == 0)
            {
                competition.ParticipantUserIdCsv = null;
                return;
            }

            var csvs = _context.Games
                .Where(g => gameIds.Contains(g.Id))
                .Select(g => g.AllowUserIdCsv)
                .ToList();

            var userIds = new HashSet<int>();
            foreach (var csv in csvs)
            {
                if (string.IsNullOrWhiteSpace(csv))
                    continue;
                foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(part.Trim(), out int uid))
                        userIds.Add(uid);
                }
            }

            competition.ParticipantUserIdCsv = userIds.Count > 0
                ? string.Join(",", userIds.OrderBy(x => x))
                : null;
        }
    }
}
