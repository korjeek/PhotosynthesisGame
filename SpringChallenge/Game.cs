namespace SpringChallenge;

public class Game
{
    public int day;
    public int nutrients;
    public List<Cell> board;
    public List<Action> possibleActions;
    public List<Tree> trees;
    public int mySun, opponentSun;
    public int myScore, opponentScore;
    public bool opponentIsWaiting;

    public Game()
    {
        board = [];
        possibleActions = [];
        trees = [];
    }

    public Action GetNextAction()
    {
        var mySize2 = trees.Count(t => t is { isMine: true, size: 2 });
        var mySize3 = trees.Count(t => t is { isMine: true, size: 3 });
        
        var completeCandidates = trees
            .Where(t => t is { isMine: true, size: 3, isDormant: false })
            .Select(t => new { t.cellIndex, Bonus = GetBonus(t.cellIndex) })
            .ToList();

        if (completeCandidates.Count != 0 && mySun >= 4)
        {
            var best = completeCandidates.OrderByDescending(c => c.Bonus).First();
            return new Action(Action.COMPLETE, best.cellIndex);
        }
        
        var costGrow23 = 7 + mySize3;
        var grow23Candidates = trees
            .Where(t => t is { isMine: true, size: 2, isDormant: false })
            .Select(t => new { t.cellIndex, Richness = GetRichness(t.cellIndex) })
            .ToList();

        if (grow23Candidates.Count != 0 && mySun >= costGrow23)
        {
            var best = grow23Candidates.OrderByDescending(c => c.Richness).First();
            return new Action(Action.GROW, best.cellIndex);
        }
        
        var costGrow12 = 3 + mySize2;
        var grow12Candidates = trees
            .Where(t => t.isMine && t is { size: 1, isDormant: false })
            .Select(t => new { t.cellIndex, Richness = GetRichness(t.cellIndex) })
            .ToList();

        if (grow12Candidates.Count != 0 && mySun >= costGrow12)
        {
            var best = grow12Candidates.OrderByDescending(c => c.Richness).First();
            return new Action(Action.GROW, best.cellIndex);
        }
        
        return new Action(Action.WAIT);
    }
    
    private int GetBonus(int cellIndex)
    {
        var richness = GetRichness(cellIndex);
        return richness switch
        {
            3 => 4,
            2 => 2,
            _ => 0
        };
    }
    
    private int GetRichness(int cellIndex)
    {
        return board.First(c => c.index == cellIndex).richness;
    }
}