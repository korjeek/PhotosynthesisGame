using System.Diagnostics;

namespace SpringChallenge;

public class MonteCarlo
{
    public static Action Run(Game game, List<Action> actions)
    {
        var sw = Stopwatch.StartNew();
        var scores = new double[actions.Count];
        var visits = new int[actions.Count];
        var iter = 0;

        while (sw.ElapsedMilliseconds < 100)
        {
            var idx = iter % actions.Count;
            var sim = game.Clone();
                
            sim.ApplyAction(actions[idx]);
            
            var limit = Math.Min(24, sim.day + 5);
            while (sim.day < limit) 
                sim.EndDay();

            scores[idx] += sim.Evaluate();
            visits[idx]++;
            iter++;
        }

        var bestIdx = 0;
        var topScore = double.MinValue;
        for (var i = 0; i < actions.Count; i++)
        {
            var avg = visits[i] == 0 ? -1e9 : scores[i] / visits[i];
            if (avg > topScore)
            {
                topScore = avg; 
                bestIdx = i;
            }
        }
        
        return actions[bestIdx];
    }
}