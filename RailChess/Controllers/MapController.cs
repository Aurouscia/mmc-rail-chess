﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RailChess.Models.DbCtx;
using RailChess.Models.Map;
using RailChess.Services;

namespace RailChess.Controllers
{
    [Authorize]
    public class MapController : Controller
    {
        private readonly RailChessContext _context;
        private readonly int _userId;
        private const string myMaps = "我上传的";

        public MapController(RailChessContext context, HttpUserIdProvider httpUserIdProvider)
        {
            _context = context;
            _userId = httpUserIdProvider.Get();
        }
        public IActionResult Index(string search)
        {
            search ??= "";
            var q = _context.Maps.AsQueryable<RailChessMap>();
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
            var list = q.OrderByDescending(x => x.UpdateTime).Select(x => new {x.Id, x.Title, x.Author, x.ImgFileName, x.UpdateTime}).Take(20).ToList();
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
                    FileName = map.ImgFileName
                });
            }
            return this.ApiResp(res);
        }
        public IActionResult CreateOrEdit(int id, string? title, IFormFile? file)
        {
            bool existing = id > 0;
            title ??= "";
            if (title.Length < 3 || title.Length > 20)
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
                if (file.Length > 1 * 1000 * 1000)
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
                public string? Author { get; set; }
                public string? Date { get; set; } 
                public string? FileName { get; set; }
            }
        }
    }
}
