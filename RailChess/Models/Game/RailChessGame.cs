namespace RailChess.Models.Game
{
    public class RailChessGame
    {
        public int Id { get; set; }
        public int HostUserId { get; set; }
        public int UseMapId { get; set; }
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

        public bool Deleted { get;set; }
    }

    public enum RandAlgType
    {
        Uniform = 0,
        Gaussian = 1
    }
}
