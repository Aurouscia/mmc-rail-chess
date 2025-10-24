using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RailChess.Models.DbCtx;
using RailChess.Services;
using RailChess.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RailChess.Controllers
{
    public class AuthController : Controller
    {
        private readonly RailChessContext _context;
        private readonly HttpUserInfoService _userInfo;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            RailChessContext context,
            HttpUserInfoService userInfo,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _context = context;
            _userInfo = userInfo;
            _config = config;
            _logger = logger;
        }
        public IActionResult Login(string? userName, string? password)
        {
            _logger.LogInformation("登录请求：{userName}", userName);

            if (userName is null || password is null)
                return this.ApiFailedResp("请填写用户名和密码");
            string pwdMd5 = MD5Helper.GetMD5Of(password);
            var u = _context.Users.Where(x => x.Name == userName && x.Pwd == pwdMd5).FirstOrDefault();
            if (u is null)
                return this.ApiFailedResp("用户名或密码错误");

            string domain = _config["Jwt:Domain"] ?? throw new Exception("未找到配置项Jwt:Domain");
            string secret = _config["Jwt:SecretKey"] ?? throw new Exception("未找到配置项Jwt:SecretKey");

            int expHours = 72;
            var claims = new[]
            {
                    new Claim (JwtRegisteredClaimNames.Nbf,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}") ,
                    new Claim (JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(DateTime.Now.AddHours(expHours)).ToUnixTimeSeconds()}"),
                    new Claim (type:JwtRegisteredClaimNames.NameId, u.Id.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: domain,
                audience: domain,
                claims: claims,
                expires: DateTime.Now.AddHours(expHours),
                signingCredentials: creds
            );

            string tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation("[{userId}]{userName}登录成功", u.Id, userName);
            return this.ApiResp(new { token = tokenStr });
        }

        public IActionResult IdentityTest()
        {
            return this.ApiResp(_userInfo);
        }
    }
}
