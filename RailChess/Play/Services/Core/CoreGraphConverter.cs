using RailChess.GraphDefinition;
using RailChess.Models.Map;

namespace RailChess.Play.Services.Core
{
    public class CoreGraphConverter
    {
        public Graph? Convert(RailChessTopo topo)
        {
            if (topo.Stations is null || topo.Lines is null)
                return null;
            Dictionary<int, Sta> ss = topo.Stations.ToDictionary
                (x => x.Id, x => new Sta(x.Id));
            topo.Lines.ForEach(line =>
            {
                if (line.Stas is not null && line.Stas.Count > 1)
                {
                    for (int i = 0; i < line.Stas.Count; i++)
                    {
                        int staId = line.Stas[i];
                        ss.TryGetValue(staId, out var target);
                        if (target is null) continue;
                        List<int> neighborHere = new(2);
                        if (i == 0)
                            neighborHere.Add(line.Stas[1]);//至少有两个站才会进来，1肯定有东西
                        else if (i == line.Stas.Count - 1)
                            neighborHere.Add(line.Stas[^2]);
                        else
                        {
                            neighborHere.Add(line.Stas[i - 1]);
                            neighborHere.Add(line.Stas[i + 1]);
                        }

                        neighborHere.ForEach(n =>
                        {
                            ss.TryGetValue(n, out var ns);
                            if (ns is not null)
                            {
                                target.Neighbors.Add(new(line.Id, ns));
                            }
                        });
                    }
                }
            });
            return new Graph(ss.Values.ToList());
        }
    }
}
