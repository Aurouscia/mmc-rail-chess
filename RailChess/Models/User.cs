namespace RailChess.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Pwd { get; set; }
        public string? AvatarName { get; set; }
        public int Elo { get; set; }
    }
}
