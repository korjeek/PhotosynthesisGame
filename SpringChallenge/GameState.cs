namespace SpringChallenge;

public class GameState
{
    public int day;
    public int nutrients;
    public List<Cell> board;
    public List<Action> possibleActions;
    public List<Tree> trees;
    public int mySun, opponentSun;
    public int myScore, opponentScore;
    public bool opponentIsWaiting;

    public GameState()
    {
        board = [];
        possibleActions = [];
        trees = [];
    }

    public GameState Clone() => new()
    {
        day = day,
        nutrients = nutrients,
        board = [..board],
        trees = trees.Select(t => t.Clone()).ToList(),
        possibleActions = possibleActions.Select(a => new Action(a.type, a.sourceCellIdx, a.targetCellIdx)).ToList(),
        mySun = mySun,
        opponentSun = opponentSun,
        myScore = myScore,
        opponentScore = opponentScore,
        opponentIsWaiting = opponentIsWaiting
    };
    
    public void ApplyAction(Action action)
    {
        if (action.type == Action.Wait)
        {
            return; 
        }

        if (action.type == Action.Grow)
        {
            var tree = trees.First(t => t.cellIndex == action.targetCellIdx);
            
            var cost = tree.size switch
            {
                0 => 1 + trees.Count(t => t.isMine && t.size == 1),
                1 => 3 + trees.Count(t => t.isMine && t.size == 2),
                2 => 7 + trees.Count(t => t.isMine && t.size == 3),
                _ => 0
            };

            mySun -= cost;
            tree.size += 1;
            tree.isDormant = true;
        }
        else if (action.type == Action.Seed)
        {
            var sourceTree = trees.First(t => t.cellIndex == action.sourceCellIdx);
            var cost = trees.Count(t => t.isMine && t.size == 0);
            
            mySun -= cost;
            sourceTree.isDormant = true;
            
            trees.Add(new Tree(action.targetCellIdx, 0, true, true));
        }
        else if (action.type == Action.Complete)
        {
            var tree = trees.First(t => t.cellIndex == action.targetCellIdx);
            mySun -= 4;
            
            var richness = GetRichness(action.targetCellIdx);
            var bonus = richness switch
            {
                3 => 4,
                2 => 2,
                _ => 0
            };
            myScore += nutrients + bonus;
            nutrients = Math.Max(0, nutrients - 1);
            
            trees.Remove(tree);
        }
    }
    
    private int GetRichness(int cellIndex) => board.First(c => c.index == cellIndex).richness;
    
    public void GeneratePossibleActions()
    {
        possibleActions = new List<Action>();
        possibleActions.Add(new Action(Action.Wait));

        // Считаем стоимость семени один раз для текущего состояния
        int seedCost = trees.Count(t => t.isMine && t.size == 0);

        foreach (var tree in trees.Where(t => t.isMine && !t.isDormant))
        {
            // 1. COMPLETE
            if (tree.size == 3 && mySun >= 4)
            {
                possibleActions.Add(new Action(Action.Complete, tree.cellIndex));
            }
        
            // 2. GROW
            if (tree.size < 3)
            {
                int growCost = GetGrowCost(tree);
                if (mySun >= growCost)
                {
                    possibleActions.Add(new Action(Action.Grow, tree.cellIndex));
                }
            }

            // 3. SEED
            // Сеять могут только деревья (размер 1+), если хватает солнца
            if (tree.size > 0 && mySun >= seedCost)
            {
                var targetIndices = GetReachableCells(tree.cellIndex, tree.size);
                foreach (var targetIdx in targetIndices)
                {
                    // Проверка: клетка должна быть пригодной и пустой
                    if (board[targetIdx].richness > 0 && trees.All(t => t.cellIndex != targetIdx))
                    {
                        possibleActions.Add(new Action(Action.Seed, tree.cellIndex, targetIdx));
                    }
                }
            }
        }
    }

    // Вспомогательный метод для расчета стоимости роста
    private int GetGrowCost(Tree tree)
    {
        return tree.size switch
        {
            0 => 1 + trees.Count(t => t is { isMine: true, size: 1 }),
            1 => 3 + trees.Count(t => t is { isMine: true, size: 2 }),
            2 => 7 + trees.Count(t => t is { isMine: true, size: 3 }),
            _ => int.MaxValue
        };
    }
    
    public List<int> GetReachableCells(int startIdx, int distance)
    {
        var reachable = new HashSet<int>();
        var currentLayer = new List<int> { startIdx };
        reachable.Add(startIdx);

        for (int i = 0; i < distance; i++)
        {
            var nextLayer = new List<int>();
            foreach (var cellIdx in currentLayer)
            {
                foreach (var neighborIdx in board[cellIdx].neighbours)
                {
                    if (neighborIdx != -1 && reachable.Add(neighborIdx))
                    {
                        nextLayer.Add(neighborIdx);
                    }
                }
            }
            currentLayer = nextLayer;
        }
        
        reachable.Remove(startIdx);
        return reachable.ToList();
    }
}