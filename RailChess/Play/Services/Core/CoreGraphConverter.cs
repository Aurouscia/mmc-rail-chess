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
            List<Sta> ss = topo.Stations.ConvertAll(x => new Sta(x.Id));
            topo.Lines.ForEach(line =>
            {
                if (line.Stas is not null && line.Stas.Count > 1)
                {
                    for (int i = 0; i < line.Stas.Count; i++)
                    {
                        int staId = line.Stas[i];
                        var target = ss.Find(x => x.Id == staId);
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
                            var ns = ss.Find(s => s.Id == n);
                            if (ns is not null)
                            {
                                target.Neighbors.Add(new(line.Id, ns));
                            }
                        });
                    }
                }
            });
            return new Graph(ss);
        }
    }
}
