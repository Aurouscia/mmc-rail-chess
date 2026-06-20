namespace RailChess.Play.PlayHubResponseModel
{
    public class SyncData
    {
        public List<Player>? PlayerStatus { get; set; }
        public int RandNumber { get; set; }
        public List<OcpStatus>? Ocps { get; set; }
        public OcpStatus? NewOcps { get; set; }
        public List<List<int>>? Selections { get; set; }
        public bool GameStarted { get; set; }
        public int LeftSecsBeforeCanKick { get; set; }
        public int TFilterId { get; set; }
    }

    public class Player
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Score { get; set; }
        public int StuckTimes { get; set; }
        public int AtSta { get; set; }
        public string? AvtFileName { get; set; }
        public bool Out { get; set; }
    }
    public class OcpStatus
    {
        public int PlayerId { get; set; }
        public List<int>? Stas { get; set; }
    }
}
