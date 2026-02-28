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
        public static string? AlgTypeName(RandAlgType type)
        {
            return type switch
            {
                RandAlgType.Uniform => "均匀",
                RandAlgType.UniformAB => "均匀双步数",
                RandAlgType.Gaussian10 => "高斯1.0",
                RandAlgType.Gaussian15 => "高斯1.5",
                RandAlgType.Gaussian20 => "高斯2.0",
                RandAlgType.Gaussian25 => "高斯2.5",
                RandAlgType.AlwaysNegativeOne => "自由移动",
                _ => "???"
            };
        }


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
            return Random.Shared.Next(min, max+1);
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
            
            // 计算边界对应的 CDF 值（使用 0.5 偏移确保包含边界）
            double pMin = Normal.CDF(mean, stddev, min - 0.5);
            double pMax = Normal.CDF(mean, stddev, max + 0.5);
            
            // 在有效概率范围内均匀采样，然后用 InvCDF 转换
            double p = pMin + Random.Shared.NextDouble() * (pMax - pMin);
            double result = Normal.InvCDF(mean, stddev, p);
            
            return (int)Math.Round(result);
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
