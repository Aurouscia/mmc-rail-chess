using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RailChess.Models.COM;
using RailChess.Models.DbCtx;
using RailChess.Models.Map;
using RailChess.Services;
using static System.Net.Mime.MediaTypeNames;

namespace RailChess.Controllers
{
    [Authorize]
    public class MapController : Controller
    {
        private const int mapBgFileMaxSize = 5 * 1000 * 1000;
        private readonly RailChessContext _context;
        private readonly int _userId;
        private const string myMaps = "我上传的";

        public MapController(RailChessContext context, HttpUserIdProvider httpUserIdProvider)
        {
            _context = context;
            _userId = httpUserIdProvider.Get();
        }
        [AllowAnonymous]
        public IActionResult Index(string search)
        {
            search ??= "";
            var q = _context.Maps.Where(x => x.Deleted == false);
            if(search.Trim() == myMaps)
                q = q.Where(x => x.Author == _userId);
            else if(!string.IsNullOrWhiteSpace(search))
            {
                var u = _context.Users.FirstOrDefault(x => x.Name == search);
                if(u is not null)
                    q = q.Where(x => x.Author == u.Id);
                else
                    q = q.Where(x => x.Title != null && x.Title.Contains(search));
            }
            var list = q.OrderByDescending(x => x.UpdateTime).Select(x => 
                new {x.Id, x.Title, x.Author, x.ImgFileName, x.LineCount,x.StationCount,x.ExcStationCount, x.UpdateTime}).Take(20).ToList();
            var authorIds = list.Select(x=>x.Author).ToList();
            var us = _context.Users.Where(x => authorIds.Contains(x.Id)).Select(x => new {x.Id,x.Name}).ToList();

            var res = new RailChessMapIndexResult();
            foreach (var map in list) 
            {
                string authorName = us.FirstOrDefault(x => x.Id == map.Author)?.Name ?? "???";
                res.Items.Add(new()
                {
                    Id = map.Id,
                    Title = map.Title,
                    Author = authorName,
                    Date = map.UpdateTime.ToString("yy-MM-dd HH:mm:ss"),
                    FileName = map.ImgFileName,
                    LineCount = map.LineCount,
                    StationCount = map.StationCount,
                    ExcStationCount = map.ExcStationCount,
                });
            }
            return this.ApiResp(res);
        }
        public IActionResult CreateOrEdit(int id, string? title, IFormFile? file)
        {
            bool existing = id > 0;
            title ??= "";
            if (title.Length < 2 || title.Length > 20)
                return this.ApiFailedResp("棋盘名称应在2-20之间");

            RailChessMap? m;
            if (existing) {
                m = _context.Maps.Find(id);
                if (m is null)
                    return this.ApiFailedResp("找不到指定的地图");

                if (m.Author != _userId)
                    return this.ApiFailedResp("只能编辑自己的棋盘");
                m.Title = title;
            }
            else
            {
                m = new()
                {
                    Title = title,
                    Author = _userId
                };
            }

            if (file is null && !existing)
                return this.ApiFailedResp("请上传棋盘背景图片");

            if (file is not null)
            {
                if (file.Length > mapBgFileMaxSize)
                    return this.ApiFailedResp("请勿上传过大图片");

                string ext = Path.GetExtension(file.FileName);
                string name = Path.GetRandomFileName();
                name = Path.ChangeExtension(name, ext);
                string dir = "./wwwroot/maps";
                var di = new DirectoryInfo(dir);
                if (!di.Exists) { di.Create(); }
                string pathName = Path.Combine(dir, name);
                var fs = System.IO.File.Create(pathName);
                file.CopyTo(fs);
                fs.Flush(); fs.Close();

                if (m.ImgFileName is not null)
                {
                    FileInfo original = new(Path.Combine(dir, m.ImgFileName));
                    if (original.Exists)
                        original.Delete();
                }
                m.ImgFileName = name;
            }

            m.UpdateTime = DateTime.Now;
            if (existing)
                _context.Maps.Update(m);
            else
                _context.Maps.Add(m);
            _context.SaveChanges();
            return this.ApiResp();
        }
        public IActionResult LoadTopo(int id)
        {
            var m = _context.Maps.Find(id);
            if (m is null)
                return this.ApiFailedResp("找不到指定棋盘");
            TopoEditorLoadResult res = new()
            {
                TopoData = m.TopoData,
                FileName = m.ImgFileName
            };
            return this.ApiResp(res);
        }
        public IActionResult SaveTopo(int id, string data)
        {
            RailChessMap? map = _context.Maps.Find(id);
            if (map is null)
                return this.ApiFailedResp("找不到指定棋盘");
            if (map.Author != _userId)
                return this.ApiFailedResp("只能编辑自己的棋盘");
            RailChessTopo? topo = JsonConvert.DeserializeObject<RailChessTopo>(data);
            if (topo is null || topo.Lines is null || topo.Stations is null)
                return this.ApiFailedResp("未知错误，请联系管理员");
            var staDirsInfo = topo.StationsDirections();
            map.LineCount = topo.Lines.Count;
            map.StationCount = staDirsInfo.Count;
            map.ExcStationCount = staDirsInfo.Values.Where(x => x > 2).Count();
            map.UpdateTime = DateTime.Now;
            map.TopoData = data;
            _context.SaveChanges();
            return this.ApiResp();
        }
        public IActionResult ImportTopo(int id, IFormFile file)
        {
            if(file is null || file.Length > 500*1000)
            {
                return BadRequest();
            }
            RailChessMap? map = _context.Maps.Find(id);
            if (map is null)
                return this.ApiFailedResp("找不到指定棋盘");
            if (map.Author != _userId)
                return this.ApiFailedResp("只能编辑自己的棋盘");
            //if (map.TopoData is not null)
            //    return this.ApiFailedResp("只能向新建棋盘导入");

            var sr = new StreamReader(file.OpenReadStream());
            string json = sr.ReadToEnd();

            RailChessTopo? topo = JsonConvert.DeserializeObject<RailChessTopo>(json);
            if (topo is null || topo.Lines is null || topo.Stations is null)
                return this.ApiFailedResp("文件格式有误");
            var staDirsInfo = topo.StationsDirections();
            map.LineCount = topo.Lines.Count;
            map.StationCount = staDirsInfo.Count;
            map.ExcStationCount = staDirsInfo.Values.Where(x => x > 2).Count();
            map.UpdateTime = DateTime.Now;
            map.TopoData = json;
            _context.SaveChanges();
            return this.ApiResp();
        }
        [AllowAnonymous]
        public IActionResult ExportTopo(int id)
        {
            RailChessMap? map = _context.Maps.Find(id);
            if (map is null)
                return this.ApiFailedResp("找不到指定棋盘");
            if (map.TopoData is null)
                return this.ApiFailedResp("棋盘数据为空");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(map.TopoData);
            return File(bytes, Application.Octet, $"{map.Title}_{DateTime.Now:MMdd_HHmm}_地图数据.json");
        }

        public IActionResult Delete(int id)
        {
            RailChessMap? map = _context.Maps.Find(id);
            if (map is null)
                return this.ApiFailedResp("找不到指定棋盘");
            if (map.Author != _userId)
                return this.ApiFailedResp("只能编辑自己的棋盘");
            map.Deleted = true;
            _context.SaveChanges();
            return this.ApiResp();
        }
        public IActionResult QuickSearch(string s)
        {
            var maps = _context.Maps
                .Where(x => !x.Deleted)
                .Where(x => x.Title != null && x.Title.Contains(s))
                .Select(x => new { x.Id,x.Title, x.UpdateTime }).Take(6).ToList();
            QuickSearchResult res = new();
            maps.ForEach(x =>
            {
                res.Items.Add(new(x.Title??"???", x.UpdateTime.ToString("yy/MM/dd HH:mm"), x.Id));
            });
            return this.ApiResp(res);
        }

        public class RailChessMapIndexResult
        {
            public RailChessMapIndexResult()
            {
                Items = new();
            }
            public List<Item> Items { get; set; }
            public class Item
            {
                public int Id { get; set; }
                public string? Title { get; set; }
                public int LineCount { get; set; }
                public int StationCount { get; set; }
                public int ExcStationCount { get; set; }
                public string? Author { get; set; }
                public string? Date { get; set; } 
                public string? FileName { get; set; }
            }
        }
        public class TopoEditorLoadResult
        {
            public string? TopoData { get; set; }
            public string? FileName { get; set; }
        }
    }
}
