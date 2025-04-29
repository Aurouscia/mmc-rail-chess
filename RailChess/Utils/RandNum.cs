using RailChess.Models.Game;

namespace RailChess.Utils
{
    public class RandNum
    {
        public static int Run(RandAlgType type, int min, int max)
        {
            if (type == RandAlgType.Uniform)
                return Uniform(min, max);
            if (type == RandAlgType.AlwaysNegativeOne)
                return -1;
            throw new NotImplementedException();
        }

        private readonly static Random rand = new();
        public static int Uniform(int min, int max)
        {
            return rand.Next(min, max+1);
        }
    }
}
