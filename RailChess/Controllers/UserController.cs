using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RailChess.Models;
using RailChess.Models.COM;
using RailChess.Models.DbCtx;
using RailChess.Play.Services;
using RailChess.Services;
using RailChess.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace RailChess.Controllers
{
    public class UserController : Controller
    {
        private readonly RailChessContext _context;
        private readonly int _userId;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public UserController(
            RailChessContext context,
            HttpUserIdProvider httpUserIdProvider,
            IMemoryCache cache,
            IConfiguration config)
        {
            _context = context;
            _userId = httpUserIdProvider.Get();
            _cache = cache;
            _config = config;
        }
        public IActionResult Add(string? userName, string? password)
        {
            var name = userName ?? "";
            var pwd = password ?? "";
            if(name.Length<1 || name.Length > 15)
            {
                return this.ApiFailedResp("用户名必须在1-15个字符");
            }
            if(pwd.Length<6 || pwd.Length > 20)
            {
                return this.ApiFailedResp("密码必须在6-20个字符");
            }
            if (_context.Users.Any(x => x.Name == name))
            {
                return this.ApiFailedResp("该用户名已经被占用");
            }
            User u = new User()
            {
                Name = name,
                Pwd = MD5Helper.GetMD5Of(pwd)
            };
            _context.Add(u);
            _context.SaveChanges();
            return this.ApiResp();
        }

        [Authorize]
        public IActionResult Edit()
        {
            if (_userId == 0)
                return this.ApiFailedResp("请先登录");
            var u = _context.Users.FirstOrDefault(x => x.Id == _userId);
            if (u is null)
                return NotFound();
            u.Pwd = "";
            return this.ApiResp(u);
        }

        [Authorize]
        public IActionResult EditExe([FromBody]User u)
        {
            if (u is null || u.Id<=0)
                return BadRequest();
            if (u.Id!=_userId)
                return this.ApiFailedResp("只能编辑自己的身份");
            if (u.Name is null || u.Name.Length < 1 || u.Name.Length > 15)
            {
                return this.ApiFailedResp("用户名必须在1-15个字符");
            }
            if(_context.Users.Any(x=>x.Id!=u.Id && x.Name == u.Name))
            {
                return this.ApiFailedResp("该用户名已被占用");
            }
            if (string.IsNullOrWhiteSpace(u.Pwd))
            {
                _context.Users.Where(x => x.Id == u.Id)
                    .ExecuteUpdate(x => x.SetProperty(a => a.Name, u.Name));
            }
            else
            {
                if (u.Pwd.Length < 6 || u.Pwd.Length > 20)
                {
                    return this.ApiFailedResp("密码必须在6-20个字符");
                }
                string pwdMd5 = MD5Helper.GetMD5Of(u.Pwd);
                _context.Users.Where(x=>x.Id==u.Id)
                    .ExecuteUpdate(x => x.SetProperty(a => a.Name, u.Name).SetProperty(a=>a.Pwd, pwdMd5));
            }
            PlayPlayerService.ClearCache(_cache, u.Id);
            return this.ApiResp();
        }

        [Authorize]
        public IActionResult SetAvatar(IFormFile avatar)
        {
            if (avatar is null || _userId == 0)
                return BadRequest();
            var u = _context.Users.Find(_userId);
            if (u is null)
                return BadRequest();
            if (avatar.Length > 5 * 1024 * 1024)
                return this.ApiFailedResp("请勿上传过大图片");
            string ext = Path.GetExtension(avatar.FileName).ToLower();
            if (!supportedAvatarExts.Contains(ext))
                return this.ApiFailedResp($"请上传{string.Join('/',supportedAvatarExts)}");

            Image img = Image.Load(avatar.OpenReadStream());
            img.Mutate(x => x.Resize(new ResizeOptions()
            {
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Size = new Size(avatarSide,avatarSide)
            }));

            string name = Path.GetRandomFileName();
            name = Path.ChangeExtension(name, "png");
            var dir = "./wwwroot/avts";
            var di = new DirectoryInfo(dir);
            if (!di.Exists)
                di.Create();
            string pathName = Path.Combine(dir, name);
            img.SaveAsPng(pathName);

            string? originalName = u.AvatarName;
            u.AvatarName = name;
            _context.SaveChanges();

            if(originalName is not null)
            {
                var originalAvt = new FileInfo(Path.Combine(dir, originalName));
                if(originalAvt.Exists)
                {
                    originalAvt.Delete();
                }
            }
            PlayPlayerService.ClearCache(_cache, u.Id);
            return this.ApiResp();
        }

        public IActionResult RecalculateElo()
        {
            // var rs = _context.GameResults.ToList();
            // var users = _context.Users.ToList();
            // foreach (var u in users)
            // {
            //     var ur = rs.FindAll(x => x.UserId == u.Id);
            //     u.Elo = ur.Select(x => x.EloDelta).Sum();
            // }
            return this.ApiResp();
        }

        public IActionResult RankingList()
        {
            var allUsers = _context.Users.ToList();
            var now = DateTime.Now;
            var oneMonthAgo = now.AddDays(-30);
            var allGameRess = (
                from res in _context.GameResults
                join game in _context.Games on res.GameId equals game.Id
                where game.StartTime > oneMonthAgo
                group res by res.UserId into g
                select new {UserId = g.Key, Count = g.Count()}
            ).ToList();
            var list = allUsers.ConvertAll(x => new UserRankingListItem()
            {
                UId = x.Id,
                UName = x.Name,
                UAvt = x.AvatarName,
            });
            foreach(var u in list)
            {
                u.Plays = allGameRess.Find(x => x.UserId == u.UId)?.Count ?? 0;
            }
            list.RemoveAll(x => x.Plays == 0 && x.UId != _userId);
            list.Sort((x, y) => {
                int xIsUser = x.UId == _userId ? 1:0;
                int yIsUser = y.UId == _userId ? 1:0;
                if (xIsUser != yIsUser)
                    return yIsUser - xIsUser;
                return y.Plays - x.Plays;
            });
            return this.ApiResp(list);
        }

        [Route("/ResetPwd/{masterKey}/{userName}")]
        public IActionResult ResetPwd(string masterKey, string userName)
        {
            var masterKeyShouldBe = _config["MasterKey"] ?? Path.GetRandomFileName();
            if (masterKeyShouldBe != masterKey)
                return this.ApiFailedResp("MasterKey错误");
            var u = _context.Users.Where(x=>x.Name == userName).FirstOrDefault();
            if(u is { })
            {
                string newPwd = RandNum.Uniform(100000, 999999).ToString();
                string pwdMd5 = MD5Helper.GetMD5Of(newPwd);
                u.Pwd = pwdMd5;
                _context.SaveChanges();
                return this.ApiResp($"已为用户 {u.Name}({u.Id}) 重置密码为 {newPwd}", success:true);
            }
            return this.ApiFailedResp("未找到该用户");
        }

        public IActionResult QuickSearch(string s)
        {
            var users = _context.Users
                .Where(x => x.Name != null && x.Name.Contains(s))
                .OrderBy(x => x.Id)
                .Take(6)
                .ToList();
            var res = new QuickSearchResult();
            foreach(var u in users)
            {
                res.Items.Add(new(u.Name ?? "??", null, u.Id));
            }
            return this.ApiResp(res);
        }

        public class UserRankingListItem
        {
            public int UId { get; set; }
            public string? UName { get; set; }
            public string? UAvt { get; set; }
            public int Plays { get; set; }
            public List<float>? Ranks { get; set; }
            public int AvgRank { get; set; }
        }

        private readonly static List<string> supportedAvatarExts = new() { ".png", ".jpg", ".jpeg" };
        private const int avatarSide = 64;
    }
}
