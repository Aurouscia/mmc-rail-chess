using MathNet.Numerics.Distributions;
using RailChess.Models.Game;

namespace RailChess.Utils
{
    public class RandNum
    {
        public static int Run(RandAlgType type, int min, int max)
        {
            return type switch
            {
                RandAlgType.Uniform => Uniform(min, max),
                RandAlgType.UniformAB => AB(min, max, Uniform),
                RandAlgType.Gaussian10 => Gaussian(min, max, 1.0),
                RandAlgType.Gaussian15 => Gaussian(min, max, 1.5),
                RandAlgType.Gaussian20 => Gaussian(min, max, 2.0),
                RandAlgType.Gaussian25 => Gaussian(min, max, 2.5),
                RandAlgType.AlwaysNegativeOne => -1,
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// 类全局共用的随机数生成器
        /// </summary>
        private readonly static Random rand = new();
        /// <summary>
        /// 均匀分布
        /// </summary>
        /// <param name="min">下界（含）</param>
        /// <param name="max">上界（含）</param>
        /// <returns>结果</returns>
        public static int Uniform(int min, int max)
        {
            if (min >= max)
                return min;
            return rand.Next(min, max+1);
        }
        /// <summary>
        /// 高斯分布（正态分布）
        /// </summary>
        /// <param name="min">下界（含）</param>
        /// <param name="max">上界（含）</param>
        /// <param name="edge">边界距离均值的标准差倍数</param>
        /// <returns></returns>
        public static int Gaussian(int min, int max, double edge)
        {
            if (min >= max)
                return min;
            double mean = (min + max) / 2.0;
            double stddev = ((max - min) / 2.0) / edge;
            Normal n = new(mean, stddev, rand);
            return LoopUntilGotInRange(min, max, n.Sample);
        }

        /// <summary>
        /// 重复尝试生成直到结果在范围内
        /// </summary>
        /// <param name="min">下界（含）</param>
        /// <param name="max">上界（含）</param>
        /// <param name="generate">生成函数</param>
        /// <returns>随机结果</returns>
        /// <exception cref="Exception">死循环</exception>
        private static int LoopUntilGotInRange(int min, int max, Func<double> generate)
        {
            int safety = 10000;
            int res;
            do
            {
                res = (int)Math.Round(generate());
                safety--;
                if (safety <= 0)
                    throw new Exception("死循环（随机数生成）");
            }
            while (res < min || res > max);
            return res;
        }
        /// <summary>
        /// 生成AB两次，返回 B*100+A
        /// </summary>
        /// <param name="min">下界（含）</param>
        /// <param name="max">上界（含）</param>
        /// <param name="generate">生成函数</param>
        /// <returns>随机结果</returns>
        private static int AB(int min, int max, Func<int, int, int> generate)
        {
            int a = generate(min, max);
            int b = generate(min, max);
            return b * 100 + a;
        }
    }
}
