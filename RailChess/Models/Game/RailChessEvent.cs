using Microsoft.EntityFrameworkCore;

namespace RailChess.Models.Game
{
    [Index(nameof(GameId))]
    public class RailChessEvent
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public RailChessEventType EventType { get; set; }
        public int PlayerId { get; set; }
        public int StationId { get; set; }
        public DateTime Time { get; set; }
        public RailChessEvent() { }
        public RailChessEvent(int gameId,RailChessEventType type,int playerId,int stationId)
        {
            GameId= gameId;
            EventType= type;
            PlayerId= playerId;
            StationId= stationId;
            Time = DateTime.Now;
        }
    }

    /// <summary>
    /// 游戏事件的类型
    /// 每次轮到一个玩家，这个玩家必然会执行MoveTo和Stuck中其一，也就是每回合有玩家数量个MoveTo/Stuck事件，用这两种事件来判断玩家先后顺序
    /// </summary>
    public enum RailChessEventType
    {
        GameStart = 10,
        GameEnd = 11,
        PlayerJoin = 20,
        PlayerMoveTo = 21,
        PlayerCapture = 22,
        PlayerStuck = 23,
        PlayerOut = 24,
        /// <summary>
        /// 表示一个产生随机数的事件，随机数会被记在RailChessEvent的StationId字段
        /// </summary>
        RandNumGened = 30,
    }
}
