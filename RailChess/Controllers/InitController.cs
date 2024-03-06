using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailChess.Models.DbCtx;

namespace RailChess.Controllers
{
    public class InitController:Controller
    {
        private readonly RailChessContext _context;

        public InitController(RailChessContext context)
        {
            _context = context;
        }

        public IActionResult Mi()
        {
            object lockObj = new();
            lock (lockObj)
            {
                try
                {
                    _context.Database.Migrate();
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
