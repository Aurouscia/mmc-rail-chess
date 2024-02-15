namespace RailChess.Utils
{
    public class RandNum
    {
        private static Random rand = new();
        public static int Uniform(int min, int max)
        {
            return rand.Next(min, max+1);
        }
    }
}
