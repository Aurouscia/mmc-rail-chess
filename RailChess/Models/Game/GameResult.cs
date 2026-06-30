using Microsoft.EntityFrameworkCore;

namespace RailChess.Models.Game
{
    [Index(nameof(GameId))]
    [Index(nameof(UserId))]
    public class GameResult
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int UserId { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; } 
        public int EloDelta { get; set; }
    }
}
