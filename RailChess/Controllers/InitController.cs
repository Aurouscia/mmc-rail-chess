using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailChess.Models.DbCtx;

namespace RailChess.Controllers
{
    public class InitController(
        RailChessContext context,
        IConfiguration config
        ) : Controller
    {
        private readonly static Lock lockObj = new();

        [Route("/Init/Mi/{masterKey}")]
        public IActionResult Mi(string masterKey)
        {
            var masterKeyShouldBe = config["MasterKey"] ?? Path.GetRandomFileName();
            if (masterKeyShouldBe != masterKey)
                return this.ApiFailedResp("MasterKey错误");
            lock (lockObj)
            {
                try
                {
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    return Ok(ex.Message + "\n" + ex.StackTrace);
                }
                return Ok("已完成");
            }
        }
    }
}
