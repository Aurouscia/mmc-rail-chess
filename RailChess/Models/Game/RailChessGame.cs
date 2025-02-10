namespace RailChess.Models.Game
{
    public class RailChessGame
    {
        public int Id { get; set; }
        public int HostUserId { get; set; }
        public int UseMapId { get; set; }
        public DateTime CreateTime { get; set; }
        public bool Started { get; set; }
        public DateTime StartTime { get; set; }
        public bool Ended { get; set; }
        public int DurationMins { get; set; }
        public int Steps { get; set; }

        public RandAlgType RandAlg { get; set; }
        public int RandMin { get; set; }
        public int RandMax { get; set; }
        public int StucksToLose { get; set; }
        public bool AllowReverseAtTerminal { get; set; }
        public int AllowTransfer { get; set; }
        public AiPlayerType AiPlayer { get; set; }
        public SpawnRuleType SpawnRule { get; set; }

        public bool Deleted { get;set; }
    }

    public enum RandAlgType
    {
        Uniform = 0,
        Gaussian = 1
    }
    public enum AiPlayerType
    {
        None = 0,
        Simple = 1,
        Medium = 2
    }
    public enum SpawnRuleType
    {
        Terminal = 0,
        TwinExchange = 1
    }
}
