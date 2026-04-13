namespace SpringChallenge;

public class Estimator
{
    public double GetScore(GameState s)
    {
        var score = 0d;

        // Очки
        score += s.myScore * 1000;

        // Солнце
        score += s.mySun * 5;

        // Деревья
        foreach (var t in s.trees.Where(t => t.isMine))
        {
            score += t.size * 10;
            var richness = s.board[t.cellIndex].richness;
            score += richness * 2;
        }
        
        score -= s.trees.Count(t => t.isMine && t.size == 0) * 3;
        
        if (s.day > 20)
        {
            foreach (var t in s.trees.Where(t => t.isMine && t.size == 3))
                score += 100;
        }

        return score;
    }
}