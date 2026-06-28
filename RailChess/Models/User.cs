using System.ComponentModel.DataAnnotations;

namespace RailChess.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Pwd { get; set; }
        public string? AvatarName { get; set; }
        public int Elo { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(128)]
        public string? ExternalIssuer { get; set; }

        [MaxLength(256)]
        public string? ExternalSubjectId { get; set; }

        public byte Type { get; set; }
    }
}
