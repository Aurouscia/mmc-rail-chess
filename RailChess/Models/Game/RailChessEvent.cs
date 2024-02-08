

namespace RailChess.Models.Game
{
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
