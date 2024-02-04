using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailChess.Models;
using RailChess.Services;
using RailChess.Utils;
using System.Xml.Linq;

namespace RailChess.Controllers
{
    public class UserController : Controller
    {
        private readonly RailChessContext _context;
        private readonly int _userId;

        public UserController(RailChessContext context, HttpUserIdProvider httpUserIdProvider)
        {
            _context = context;
            _userId = httpUserIdProvider.Get();
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
            return this.ApiResp();
        }
    }
}
