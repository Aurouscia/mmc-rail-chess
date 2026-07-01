using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Play.Services;
using RailChess.Play.Services.Core;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RailChess.Controllers
{
    public class EmbedController : Controller
    {
        private readonly RailChessContext _context;
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _toposService;
        private readonly CoreGraphProvider _graphProvider;
        private readonly IMemoryCache _cache;
        private const int DefaultCount = 10;
        private const int MaxCount = 20;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(20);

        public EmbedController(
            RailChessContext context,
            PlayEventsService eventsService,
            PlayToposService toposService,
            CoreGraphProvider graphProvider,
            IMemoryCache cache)
        {
            _context = context;
            _eventsService = eventsService;
            _toposService = toposService;
            _graphProvider = graphProvider;
            _cache = cache;
        }

        /// <summary>
        /// 当前进行中的对局
        /// GET /api/embed/active?count=10&theme=light
        /// </summary>
        public IActionResult Active(int count = DefaultCount, string theme = "light")
        {
            count = Math.Clamp(count, 1, MaxCount);
            string cacheKey = CacheKey("active", count, theme);
            if (_cache.TryGetValue(cacheKey, out string? cachedHtml) && cachedHtml is not null)
                return Content(cachedHtml, "text/html; charset=utf-8");

            var eventTimeout = TimeSpan.FromMinutes(60);
            var cutoff = DateTime.Now - eventTimeout;

            // 进行中的对局：按最新事件时间过滤，按开始时间倒序
            var startedGames = (
                from g in _context.Games
                where g.Started && !g.Ended && !g.Deleted
                let latestEvent = _context.Events
                    .Where(e => e.GameId == g.Id)
                    .OrderByDescending(e => e.Id)
                    .Select(e => (DateTime?)e.Time)
                    .FirstOrDefault()
                where latestEvent > cutoff || (latestEvent == null && g.CreateTime > cutoff)
                orderby g.StartTime descending
                select g
            ).ToList();

            // 等人中的对局：不受事件时间过滤，按创建时间倒序
            var waitingGames = (
                from g in _context.Games
                where !g.Started && !g.Ended && !g.Deleted
                orderby g.CreateTime descending
                select g
            ).ToList();

            var games = startedGames.Concat(waitingGames).Take(count).ToList();

            var userIds = games.Select(x => x.HostUserId).ToList();
            var mapIds = games.Select(x => x.UseMapId).ToList();
            var users = _context.Users.Where(x => userIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToList();
            var maps = _context.Maps.Where(x => mapIds.Contains(x.Id)).Select(x => new { x.Id, x.Title }).ToList();

            var items = games.Select(g =>
            {
                var mapName = maps.FirstOrDefault(m => m.Id == g.UseMapId)?.Title;
                var hostName = users.FirstOrDefault(u => u.Id == g.HostUserId)?.Name;
                string status = g.Started
                    ? $"已开始 {(int)(DateTime.Now - g.StartTime).TotalMinutes} 分钟"
                    : "正在等人";
                string? title = !string.IsNullOrWhiteSpace(g.GameName) ? g.GameName : mapName;
                string? subTitle = !string.IsNullOrWhiteSpace(g.GameName) ? mapName : null;
                string url = $"/#/play/{g.Id}";

                var players = GetActivePlayers(g.Id);
                return new WidgetItem(title, subTitle, hostName, status, url, players);
            }).ToList();

            string html = BuildHtml("当前进行中的对局", theme, items);
            _cache.Set(CacheKey("active", count, theme), html, CacheExpiration);
            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// 最新完成的对局
        /// GET /api/embed/recent?count=10&theme=light
        /// </summary>
        public IActionResult Recent(int count = DefaultCount, string theme = "light")
        {
            count = Math.Clamp(count, 1, MaxCount);
            string cacheKey = CacheKey("recent", count, theme);
            if (_cache.TryGetValue(cacheKey, out string? cachedHtml) && cachedHtml is not null)
                return Content(cachedHtml, "text/html; charset=utf-8");

            var games = _context.Games
                .Where(x => x.Ended && !x.Deleted && string.IsNullOrWhiteSpace(x.AllowUserIdCsv))
                .OrderByDescending(x => x.StartTime)
                .Take(count)
                .ToList();

            var gameIds = games.Select(x => x.Id).ToList();
            var mapIds = games.Select(x => x.UseMapId).ToList();
            var maps = _context.Maps.Where(x => mapIds.Contains(x.Id)).Select(x => new { x.Id, x.Title }).ToList();

            var allResults = (
                from r in _context.GameResults
                join u in _context.Users on r.UserId equals u.Id
                where gameIds.Contains(r.GameId)
                orderby r.GameId, r.Score descending
                select new { r.GameId, u.Name, r.Score }
            ).ToList();

            var items = games.Select(g =>
            {
                var mapName = maps.FirstOrDefault(m => m.Id == g.UseMapId)?.Title;
                var players = allResults
                    .Where(r => r.GameId == g.Id)
                    .Select(r => new PlayerLine(r.Name ?? "???", r.Score))
                    .ToList();

                var metaParts = new List<string>
                {
                    g.StartTime.ToString("yyyy/MM/dd HH:mm"),
                    $"{players.Count} 人参与"
                };

                string? title = !string.IsNullOrWhiteSpace(g.GameName) ? g.GameName : mapName;
                string? subTitle = !string.IsNullOrWhiteSpace(g.GameName) ? mapName : null;
                string url = $"/#/playback/{g.Id}";
                return new WidgetItem(title, subTitle, null, string.Join(" · ", metaParts), url, players);
            }).ToList();

            string html = BuildHtml("最新完成对局", theme, items);
            _cache.Set(CacheKey("recent", count, theme), html, CacheExpiration);
            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// 当前比赛中各对局的实时状况 widget
        /// GET /api/embed/widget?competitionId=1&theme=light
        /// </summary>
        public IActionResult Widget(int competitionId, string theme = "light")
        {
            string cacheKey = $"embed:widget:{competitionId}:{theme?.ToLowerInvariant() ?? "light"}";
            if (_cache.TryGetValue(cacheKey, out string? cachedHtml) && cachedHtml is not null)
                return Content(cachedHtml, "text/html; charset=utf-8");

            var competition = _context.Competitions
                .Include(x => x.Matches)
                .ThenInclude(x => x.Game)
                .FirstOrDefault(x => x.Id == competitionId && !x.Deleted);

            if (competition is null)
                return Content("<div style='padding:12px;'>比赛不存在</div>", "text/html; charset=utf-8");

            var games = competition.Matches
                .Where(m => m.Game is not null && !m.Game.Deleted)
                .Select(m => m.Game!)
                .ToList();

            var mapIds = games.Select(g => g.UseMapId).Distinct().ToList();
            var maps = _context.Maps
                .Where(m => mapIds.Contains(m.Id))
                .ToDictionary(m => m.Id, m => m.Title);

            var hostIds = games.Select(g => g.HostUserId).Distinct().ToList();
            var hosts = _context.Users
                .Where(u => hostIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name);

            var items = competition.Matches
                .Where(m => m.Game is not null && !m.Game.Deleted)
                .Select(m => new { Match = m, Game = m.Game! })
                .OrderBy(x => x.Game.Started
                    ? (x.Game.Ended ? 2 : 0) // 进行中 0，已结束 2
                    : 1)                     // 未开赛 1
                .ThenByDescending(x => x.Game.Id)
                .Select(x =>
                {
                    var m = x.Match;
                    var game = x.Game;
                    string mapName = maps.GetValueOrDefault(game.UseMapId) ?? "???";
                    string? title = !string.IsNullOrWhiteSpace(game.GameName) ? game.GameName : mapName;
                    string? subTitle = !string.IsNullOrWhiteSpace(game.GameName) ? mapName : null;
                    string hostName = hosts.GetValueOrDefault(game.HostUserId) ?? "???";

                    string status;
                    string url;
                    List<PlayerLine> players;

                    if (!game.Started)
                    {
                        status = "正在等人";
                        url = $"/#/play/{game.Id}";
                        players = new List<PlayerLine>();
                    }
                    else if (!game.Ended)
                    {
                        status = $"进行中 {(int)(DateTime.Now - game.StartTime).TotalMinutes} 分钟";
                        url = $"/#/play/{game.Id}";
                        players = GetActivePlayers(game.Id);
                    }
                    else
                    {
                        status = $"已结束 · {game.StartTime:yyyy/MM/dd HH:mm}";
                        url = $"/#/playback/{game.Id}";
                        players = GetFinalPlayers(game.Id);
                    }

                    return new WidgetItem(title, subTitle, hostName, status, url, players, m.Stage);
                })
                .ToList();

            string header = $"{competition.Title ?? "比赛"} · {GetStatusText(competition.Status)}";
            string html = BuildHtml(header, theme ?? "light", items);
            _cache.Set(cacheKey, html, CacheExpiration);
            return Content(html, "text/html; charset=utf-8");
        }

        private List<PlayerLine> GetFinalPlayers(int gameId)
        {
            var results = (
                from r in _context.GameResults
                join u in _context.Users on r.UserId equals u.Id
                where r.GameId == gameId
                orderby r.Score descending
                select new { u.Name, r.Score }
            ).ToList();

            return results
                .Select(r => new PlayerLine(r.Name ?? "???", r.Score))
                .ToList();
        }

        /// <summary>
        /// 比赛参与者积分榜 widget
        /// GET /api/embed/participants?competitionId=1&theme=light
        /// </summary>
        public IActionResult Participants(int competitionId, string theme = "light")
        {
            string cacheKey = $"embed:participants:{competitionId}:{theme?.ToLowerInvariant() ?? "light"}";
            if (_cache.TryGetValue(cacheKey, out string? cachedHtml) && cachedHtml is not null)
                return Content(cachedHtml, "text/html; charset=utf-8");

            var competition = _context.Competitions
                .Include(x => x.Matches)
                .ThenInclude(x => x.Game)
                .FirstOrDefault(x => x.Id == competitionId && !x.Deleted);

            if (competition is null)
                return Content("<div style='padding:12px;'>比赛不存在</div>", "text/html; charset=utf-8");

            var participants = JsonSerializer.Deserialize<List<CompetitionParticipant>>(
                competition.ParticipantsJson ?? "[]") ?? new List<CompetitionParticipant>();

            var participantUserIds = participants.Select(p => p.UserId).ToList();
            var userNames = _context.Users
                .Where(u => participantUserIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name);

            var endedMatches = competition.Matches
                .Where(m => m.Game is not null && !m.Game.Deleted && m.Game.Ended)
                .ToList();

            var gameIds = endedMatches.Select(m => m.GameId).ToList();
            var allResults = _context.GameResults
                .Where(r => gameIds.Contains(r.GameId))
                .Select(r => new { r.GameId, r.UserId, r.Rank })
                .ToList();
            var resultsByGame = allResults.GroupBy(r => r.GameId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var pointsByUser = participants.ToDictionary(p => p.UserId, _ => 0);
            var participantSet = participantUserIds.ToHashSet();

            foreach (var match in endedMatches)
            {
                if (!resultsByGame.TryGetValue(match.GameId, out var results))
                    continue;

                var rules = ParseScoringRules(match.ScoringJson);
                int playerCount = results.Count;
                if (!rules.TryGetValue(playerCount, out var points))
                    continue;

                foreach (var result in results)
                {
                    if (!participantSet.Contains(result.UserId))
                        continue;
                    int index = result.Rank - 1;
                    if (index < 0 || index >= points.Count)
                        continue;
                    pointsByUser[result.UserId] += points[index];
                }
            }

            var displayItems = participants
                .Select(p => new ParticipantDisplay(
                    userNames.TryGetValue(p.UserId, out var name) ? name ?? "???" : "???",
                    p.Number,
                    pointsByUser.GetValueOrDefault(p.UserId)))
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Number, StringComparer.Ordinal)
                .ThenBy(p => p.Name)
                .ToList();

            string header = $"{competition.Title ?? "比赛"} · 积分榜";
            string html = BuildParticipantHtml(header, theme ?? "light", displayItems);
            _cache.Set(cacheKey, html, CacheExpiration);
            return Content(html, "text/html; charset=utf-8");
        }

        private static Dictionary<int, List<int>> ParseScoringRules(string? scoringJson)
        {
            if (string.IsNullOrWhiteSpace(scoringJson))
                return new Dictionary<int, List<int>>();

            try
            {
                var scoring = JsonSerializer.Deserialize<CompetitionMatchScoring>(scoringJson);
                if (scoring?.Rules is null)
                    return new Dictionary<int, List<int>>();

                return scoring.Rules
                    .Where(r => r.PlayerCount > 0 && r.Points is not null)
                    .ToDictionary(r => r.PlayerCount, r => r.Points);
            }
            catch
            {
                return new Dictionary<int, List<int>>();
            }
        }

        private static string GetStatusText(CompetitionStatus status)
            => status switch
            {
                CompetitionStatus.Planned => "未开始",
                CompetitionStatus.Ongoing => "进行中",
                CompetitionStatus.Completed => "已结束",
                CompetitionStatus.Cancelled => "已取消",
                _ => status.ToString()
            };

        private List<PlayerLine> GetActivePlayers(int gameId)
        {
            try
            {
                _eventsService.GameId = gameId;
                _toposService.GameId = gameId;

                var captureEvents = _eventsService.PlayerCaptureEvents();
                var outPlayerIds = _eventsService.PlayerOutEvents()
                    .Select(x => x.PlayerId)
                    .ToHashSet();
                var playerIds = _eventsService.PlayersJoinEvents()
                    .Select(x => x.PlayerId)
                    .Distinct()
                    .ToList();

                var userNames = _context.Users
                    .Where(x => playerIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.Name })
                    .ToDictionary(x => x.Id, x => x.Name);

                var dirDict = _graphProvider.StationDirections();

                return playerIds
                    .Select(pid =>
                    {
                        var capturedStations = captureEvents
                            .Where(e => e.PlayerId == pid)
                            .Select(e => e.StationId)
                            .Distinct()
                            .ToList();
                        int score = _graphProvider.TotalDirections(capturedStations, dirDict);
                        string name = userNames.TryGetValue(pid, out var n) ? n ?? "???" : "???";
                        return new PlayerLine(name, score, outPlayerIds.Contains(pid));
                    })
                    .OrderByDescending(p => p.Score)
                    .ToList();
            }
            catch
            {
                return new List<PlayerLine>();
            }
        }

        private static string BuildParticipantHtml(string header, string theme, List<ParticipantDisplay> participants)
        {
            var (bg, fg, muted, border, hover) = theme?.ToLowerInvariant() switch
            {
                "dark" => ("#1a1a1a", "#f0f0f0", "#aaa", "#444", "#2a2a2a"),
                "transparent" => ("transparent", "#333", "#666", "#ddd", "rgba(0,0,0,0.05)"),
                _ => ("#ffffff", "#333", "#666", "#eee", "#f8f8f8")
            };

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine("<style>");
            sb.AppendLine("*, *::before, *::after { box-sizing: border-box; }");
            sb.AppendLine($"body {{ margin: 0; font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif; background: {bg}; color: {fg}; }}");
            sb.AppendLine(".widget { padding: 12px; }");
            sb.AppendLine($".header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; color: {fg}; }}");
            sb.AppendLine(".brand { display: flex; align-items: center; gap: 6px; }");
            sb.AppendLine(".brand img { width: 20px; height: 20px; display: block; }");
            sb.AppendLine(".brand span { font-size: 14px; font-weight: 600; }");
            sb.AppendLine($".header-title {{ font-size: 12px; color: {muted}; font-weight: normal; }}");
            sb.AppendLine(".list { display: flex; flex-direction: column; gap: 8px; }");
            sb.AppendLine($".item {{ padding: 10px; border: 1px solid {border}; border-radius: 8px; }}");
            sb.AppendLine(".title { font-weight: 600; font-size: 14px; margin-bottom: 2px; line-height: 1.3; }");
            sb.AppendLine($".meta {{ font-size: 12px; color: {muted}; line-height: 1.4; }}");
            sb.AppendLine(".empty { font-size: 13px; color: #999; text-align: center; padding: 24px 0; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"widget\">");
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<div class=\"brand\"><img src=\"/railchessLogo.svg\" alt=\"\" /><span>轨交棋</span></div>");
            sb.AppendLine($"<div class=\"header-title\">{HtmlEncode(header)}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"list\">");

            if (participants.Count == 0)
            {
                sb.AppendLine("<div class=\"empty\">暂无参赛选手</div>");
            }
            else
            {
                foreach (var p in participants)
                {
                    sb.AppendLine("<div class=\"item\">");
                    sb.AppendLine($"<div class=\"title\">{HtmlEncode(p.Name)}</div>");
                    string numberText = string.IsNullOrWhiteSpace(p.Number) ? "-" : p.Number;
                    sb.AppendLine($"<div class=\"meta\">参赛编号 {HtmlEncode(numberText)} · {p.Score} 积分</div>");
                    sb.AppendLine("</div>");
                }
            }

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private static string BuildHtml(string header, string theme, List<WidgetItem> items)
        {
            var (bg, fg, muted, border, hover) = theme?.ToLowerInvariant() switch
            {
                "dark" => ("#1a1a1a", "#f0f0f0", "#aaa", "#444", "#2a2a2a"),
                "transparent" => ("transparent", "#333", "#666", "#ddd", "rgba(0,0,0,0.05)"),
                _ => ("#ffffff", "#333", "#666", "#eee", "#f8f8f8")
            };

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine("<style>");
            sb.AppendLine("*, *::before, *::after { box-sizing: border-box; }");
            sb.AppendLine($"body {{ margin: 0; font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif; background: {bg}; color: {fg}; }}");
            sb.AppendLine(".widget { padding: 12px; }");
            sb.AppendLine($".header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; color: {fg}; }}");
            sb.AppendLine(".brand { display: flex; align-items: center; gap: 6px; }");
            sb.AppendLine(".brand img { width: 20px; height: 20px; display: block; }");
            sb.AppendLine(".brand span { font-size: 14px; font-weight: 600; }");
            sb.AppendLine($".header-title {{ font-size: 12px; color: {muted}; font-weight: normal; }}");
            sb.AppendLine(".list { display: flex; flex-direction: column; gap: 8px; }");
            sb.AppendLine($".item {{ display: block; text-decoration: none; color: inherit; padding: 10px; border: 1px solid {border}; border-radius: 8px; transition: background .15s; }}");
            sb.AppendLine($".item:hover {{ background: {hover}; }}");
            sb.AppendLine(".title { font-weight: 600; font-size: 14px; margin-bottom: 2px; line-height: 1.3; }");
            sb.AppendLine($".subtitle {{ font-size: 12px; color: {muted}; margin-bottom: 4px; line-height: 1.3; }}");
            sb.AppendLine($".stage {{ display: inline-block; font-size: 11px; color: {muted}; background: {hover}; padding: 1px 6px; border-radius: 4px; margin-bottom: 4px; }}");
            sb.AppendLine($".meta {{ font-size: 12px; color: {muted}; line-height: 1.4; margin-bottom: 6px; }}");
            sb.AppendLine(".players { display: flex; flex-direction: column; gap: 3px; }");
            sb.AppendLine($".player {{ font-size: 12px; color: {muted}; line-height: 1.4; }}");
            sb.AppendLine($".player.out {{ color: {muted}; opacity: 0.55; }}");
            sb.AppendLine(".empty { font-size: 13px; color: #999; text-align: center; padding: 24px 0; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"widget\">");
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<div class=\"brand\"><img src=\"/railchessLogo.svg\" alt=\"\" /><span>轨交棋</span></div>");
            sb.AppendLine($"<div class=\"header-title\">{HtmlEncode(header)}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"list\">");

            if (items.Count == 0)
            {
                sb.AppendLine("<div class=\"empty\">暂无可展示的对局</div>");
            }
            else
            {
                foreach (var item in items)
                {
                    sb.AppendLine($"<a class=\"item\" href=\"{item.Url}\" target=\"_blank\">");
                    if (!string.IsNullOrWhiteSpace(item.Stage))
                        sb.AppendLine($"<div class=\"stage\">{HtmlEncode(item.Stage)}</div>");
                    sb.AppendLine($"<div class=\"title\">{HtmlEncode(item.Title)}</div>");
                    if (!string.IsNullOrWhiteSpace(item.SubTitle))
                        sb.AppendLine($"<div class=\"subtitle\">{HtmlEncode(item.SubTitle)}</div>");

                    var metaParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(item.HostName))
                        metaParts.Add($"房主 {item.HostName}");
                    metaParts.Add(item.Status);
                    sb.AppendLine($"<div class=\"meta\">{HtmlEncode(string.Join(" · ", metaParts))}</div>");

                    if (item.Players.Count > 0)
                    {
                        sb.AppendLine("<div class=\"players\">");
                        foreach (var player in item.Players)
                        {
                            string outClass = player.IsOut ? "out" : "";
                            sb.AppendLine($"<div class=\"player {outClass}\">{HtmlEncode(player.Name)} · {player.Score} 分</div>");
                        }
                        sb.AppendLine("</div>");
                    }
                    sb.AppendLine("</a>");
                }
            }

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private static string CacheKey(string action, int count, string theme)
            => $"embed:{action}:{count}:{theme?.ToLowerInvariant() ?? "light"}";

        private static string HtmlEncode(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

        private record WidgetItem(string? Title, string? SubTitle, string? HostName, string Status, string Url, List<PlayerLine> Players, string? Stage = null);
        private record PlayerLine(string Name, int Score, bool IsOut = false);
        private record ParticipantDisplay(string Name, string? Number, int Score);
    }
}
