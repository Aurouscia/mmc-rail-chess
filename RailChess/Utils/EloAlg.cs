namespace RailChess.Utils
{
    public static class EloAlg
    {
        private const double K = 1d / 200d;
        public static int Delta(int eloA, int eloB, int scoreA, int scoreB)
        {
            double ra = eloA / 1000000d;
            double rb = eloB / 1000000d;
            double wab = 0;
            if(scoreA > scoreB) 
                wab = 1;
            else if(scoreA < scoreB)
                wab = -1;
            double eab = Math.Tanh((ra - rb));
            double delta = K * (wab - eab);
            return (int)(delta * 1000000);
        }
    }
}
