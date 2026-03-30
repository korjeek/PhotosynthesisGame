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
        var myTreesIdx = trees
            .Where(t => t.isMine)
            .Select(t => t.cellIndex)
            .ToHashSet();

        var bestCell = -1;
        var bestRichness = int.MinValue;
        foreach (var cell in board.Where(cell => myTreesIdx.Contains(cell.index) && cell.richness > bestRichness))
        {
            bestCell = cell.index;
            bestRichness = cell.richness;
        }

        return bestCell == -1 ? 
            new Action(Action.WAIT) : 
            new Action(Action.COMPLETE, bestCell);
    }
}