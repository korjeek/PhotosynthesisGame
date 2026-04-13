using System.Diagnostics;

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
        // 1. Если можем ЗАВЕРШИТЬ дерево (COMPLETE) — делаем это почти всегда (как в твоем baseline)
        var completes = possibleActions.Where(a => a.type == Action.COMPLETE).ToList();
        if (completes.Count != 0) {
            return completes
                .OrderByDescending(a => GameState.Board[a.targetCellIdx].richness)
                .First();
        }

        // 2. Ограничиваем семена (SEED), чтобы бот не спамил ими как сумасшедший
        var mySeeds = trees.Count(t => t is { isMine: true, size: 0 });
        var filteredActions = possibleActions
            .Where(a => a.type != Action.SEED || mySeeds < 1)
            .ToList();

        if (filteredActions.Count == 1) 
            return filteredActions[0];
        if (filteredActions.Count == 0) 
            return possibleActions.First(a => a.type == Action.WAIT);
        
        return MonteCarlo.Run(this, filteredActions);
    }
    
    public void ApplyAction(Action action)
    {
        if (action.type == Action.WAIT) 
            return;
        
        var targetTree = trees
            .FirstOrDefault(t => t.cellIndex == action.targetCellIdx);

        switch (action.type)
        {
            case Action.GROW when targetTree != null:
                targetTree.size++;
                targetTree.isDormant = true;
                break;
            case Action.SEED:
            {
                var sourceTree = trees.FirstOrDefault(t => t.cellIndex == action.sourceCellIdx);
                if (sourceTree != null) 
                    sourceTree.isDormant = true;
                trees.Add(new Tree(action.targetCellIdx, 0, true, true));
                break;
            }
            case Action.COMPLETE when targetTree != null:
            {
                trees.Remove(targetTree);
                var bonus = board[action.targetCellIdx].richness switch
                {
                    3 => 4,
                    2 => 2,
                    _ => 0
                };
                myScore += nutrients + bonus;
                nutrients = Math.Max(0, nutrients - 1);
                break;
            }
        }
    }
    
    public void EndDay()
    {
        var sunDir = day % 6;
        var shadowLevel = new int[37];
        foreach (var tree in trees)
        {
            var current = tree.cellIndex;
            for (var i = 1; i <= tree.size; i++)
            {
                current = board[current].neighbours[sunDir];
                if (current == -1) 
                    break;
                shadowLevel[current] = Math.Max(shadowLevel[current], tree.size);
            }
        }
        foreach (var tree in trees)
        {
            if (shadowLevel[tree.cellIndex] < tree.size)
            {
                if (tree.isMine) mySun += tree.size;
                else opponentSun += tree.size;
            }
            tree.isDormant = false;
        }
        day++;
    }
    
    public double Evaluate()
    {
        double score = myScore - opponentScore;
        foreach (var tree in trees)
        {
            var val = tree.size * 4 + board[tree.cellIndex].richness;
            if (tree.isMine) 
                score += val; 
            else 
                score -= val;
        }
        return score + mySun / 3.0 - opponentSun / 3.0;
    }

    public Game Clone()
    {
        var clone = new Game {
            day = day, 
            nutrients = nutrients, 
            board = board,
            mySun = mySun, 
            opponentSun = opponentSun,
            myScore = myScore, 
            opponentScore = opponentScore,
            trees = new List<Tree>(trees.Select(t => t.Clone()))
        };
        
        return clone;
    }
}