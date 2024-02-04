using RailChess.Models;
using System.IdentityModel.Tokens.Jwt;

namespace RailChess.Services
{
    public class HttpUserInfoService
    {
        public int Id { get; } = 0;
        public string Name { get; } = string.Empty;
        public int LeftHours { get; } = 0;
        public HttpUserInfoService(IHttpContextAccessor httpContextAccessor, HttpUserIdProvider userId, RailChessContext context)
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx is null)
                return;
            Id = userId.Get();
            if (Id <= 0)
                return;
            else
            {
                var user = context.Users.Where(x => x.Id == this.Id).FirstOrDefault();
                if (user is not null)
                {
                    Name = user.Name ?? "";
                }
                var expClaim = ctx.User.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp);
                if (expClaim is not null && long.TryParse(expClaim.Value, out long exp))
                {
                    long now = DateTimeOffset.Now.ToUnixTimeSeconds();
                    long leftSeconds = exp - now;
                    LeftHours = (int)TimeSpan.FromSeconds(leftSeconds).TotalHours;
                }
            }
        }
    }
    public class HttpUserIdProvider
    {
        private int Id { get; }
        public int Get() => Id;
        public HttpUserIdProvider(IHttpContextAccessor httpContextAccessor)
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx is null)
                return;
            var idClaim = ctx.User.Claims.FirstOrDefault(x => x.Type.Contains(JwtRegisteredClaimNames.NameId));
            if (idClaim is null)
                return;
            else
            {
                if (int.TryParse(idClaim.Value, out int id))
                {
                    Id = id;
                }
            }
        }
    }
}
