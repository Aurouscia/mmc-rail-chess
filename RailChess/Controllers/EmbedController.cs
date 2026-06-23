using Microsoft.AspNetCore.Mvc;
using RailChess.Models.DbCtx;
using RailChess.Play.Services;
using RailChess.Play.Services.Core;
using System.Net;
using System.Text;

namespace RailChess.Controllers
{
    public class EmbedController : Controller
    {
        private readonly RailChessContext _context;
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _toposService;
        private readonly CoreGraphProvider _graphProvider;
        private const int DefaultCount = 10;
        private const int MaxCount = 20;

        public EmbedController(
            RailChessContext context,
            PlayEventsService eventsService,
            PlayToposService toposService,
            CoreGraphProvider graphProvider)
        {
            _context = context;
            _eventsService = eventsService;
            _toposService = toposService;
            _graphProvider = graphProvider;
        }

        /// <summary>
        /// 当前进行中的对局
        /// GET /api/embed/active?count=10&theme=light
        /// </summary>
        public IActionResult Active(int count = DefaultCount, string theme = "light")
        {
            count = Math.Clamp(count, 1, MaxCount);
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
            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// 最新完成的对局
        /// GET /api/embed/recent?count=10&theme=light
        /// </summary>
        public IActionResult Recent(int count = DefaultCount, string theme = "light")
        {
            count = Math.Clamp(count, 1, MaxCount);

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
            return Content(html, "text/html; charset=utf-8");
        }

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

        private static string HtmlEncode(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

        private record WidgetItem(string? Title, string? SubTitle, string? HostName, string Status, string Url, List<PlayerLine> Players);
        private record PlayerLine(string Name, int Score, bool IsOut = false);
    }
}
