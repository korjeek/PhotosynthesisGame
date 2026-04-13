using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringChallenge;

public class Action
{
    public const string Wait = "WAIT";
    public const string Seed = "SEED";
    public const string Grow = "GROW";
    public const string Complete = "COMPLETE";
    
    public readonly string type;
    public readonly int targetCellIdx;
    public readonly int sourceCellIdx;

    public static Action Parse(string action)
    {
        var parts = action.Split(" ");
        switch (parts[0])
        {
            case Wait:
                return new Action(Wait);
            case Seed:
                return new Action(Seed, int.Parse(parts[1]), int.Parse(parts[2]));
            case Grow:
            case Complete:
            default:
                return new Action(parts[0], int.Parse(parts[1]));
        }
    }

    public Action(string type, int sourceCellIdx = 0, int targetCellIdx = 0)
    {
        this.type = type;
        this.targetCellIdx = targetCellIdx;
        this.sourceCellIdx = sourceCellIdx;
    }

    public Action(string type, int targetCellIdx)
        : this(type, 0, targetCellIdx)
    { }
}

public class Cell
{
    public int index;
    public int richness;
    public int[] neighbours;

    public Cell(int index, int richness, int[] neighbours)
    {
        this.index = index;
        this.richness = richness;
        this.neighbours = neighbours;
    }
}

public class Countdown
{
    private readonly Stopwatch _sw;
    private readonly long _timeLimitMs;

    public Countdown(long timeLimitMs)
    {
        _timeLimitMs = timeLimitMs;
        _sw = Stopwatch.StartNew();
    }

    public bool IsFinished() => _sw.ElapsedMilliseconds >= _timeLimitMs;
}

public class Estimator
{
    public double GetScore(GameState state)
    {
        double score = 0;
        
        score += state.myScore * 100;
        score += state.mySun * 1.5;
        
        var (myFutureSun, oppFutureSun) = SunCalculator.Calculate(state);
        score += myFutureSun * 2.0;
        score -= oppFutureSun * 1.0;
        
        var gameProgress = state.day / 24.0;

        foreach (var tree in state.trees.Where(t => t.isMine))
        {
            double treeValue = 0;
            
            switch (tree.size)
            {
                case 0: treeValue += 1; break;
                case 1: treeValue += 10; break;
                case 2: treeValue += 30; break;
                case 3: treeValue += 60; break;
            }
            
            treeValue += state.board[tree.cellIndex].richness * 5;
            
            if (gameProgress < 0.6) 
            {
                if (tree.size > 0) treeValue *= 1.2; 
            }
            else
            {
                switch (tree.size)
                {
                    case 3:
                        treeValue *= 2.0;
                        break;
                    case 0:
                        treeValue *= 0.2;
                        break;
                }
            }

            score += treeValue;
        }
        
        if (state.trees.Count(t => t.isMine) < 3) score -= 50;

        return score;
    }
}

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

public class GreedySolver
{
    private readonly Estimator _estimator = new();

    public Action GetSolution(GameState problem, Countdown countdown)
    {
        var possibleActions = problem.possibleActions;
        
        if (possibleActions is [{ type: Action.Wait }])
            return possibleActions[0];
        
        var bestAction = possibleActions[0];
        var bestScore = double.MinValue;

        foreach (var action in possibleActions)
        {
            if (countdown.IsFinished())
                break;

            var stateAfterFirstMove = problem.Clone();
            stateAfterFirstMove.ApplyAction(action);

            var finalState = Simulate(stateAfterFirstMove, 5);
            var finalScore = _estimator.GetScore(finalState);
            
            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestAction = action;
            }
        }

        return bestAction;
    }

    private GameState Simulate(GameState current, int depth)
    {
        for (var d = 0; d < depth; d++)
        {
            current.GeneratePossibleActions();
            var possibleActions = current.possibleActions;
            
            if (possibleActions.Count == 0)
                break;
            
            var bestAction = possibleActions[0];
            var bestScore = double.MinValue;
            
            foreach (var action in possibleActions)
            {
                if (action.type == Action.Wait) 
                    continue;
                
                var s = current.Clone();
                s.ApplyAction(action);
                
                var score = _estimator.GetScore(s);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAction = action;
                }
            }

            current.ApplyAction(bestAction);
        }
        
        return current;
    }
}

public class Player
{
    private static readonly GreedySolver Solver = new();
    
    public static void Main(string[] args)
    {
        string[] inputs;

        var gameState = new GameState();

        var numberOfCells = int.Parse(Console.ReadLine()!); // 37
        for (var i = 0; i < numberOfCells; i++)
        {
            inputs = Console.ReadLine()!.Split(' ');
            var index = int.Parse(inputs[0]); // 0 is the center cell, the next cells spiral outwards
            var richness = int.Parse(inputs[1]); // 0 if the cell is unusable, 1-3 for usable cells
            var neigh0 = int.Parse(inputs[2]); // the index of the neighbouring cell for each direction
            var neigh1 = int.Parse(inputs[3]);
            var neigh2 = int.Parse(inputs[4]);
            var neigh3 = int.Parse(inputs[5]);
            var neigh4 = int.Parse(inputs[6]);
            var neigh5 = int.Parse(inputs[7]);
            var neighs = new[] { neigh0, neigh1, neigh2, neigh3, neigh4, neigh5 };
            var cell = new Cell(index, richness, neighs);
            gameState.board.Add(cell);
        }

        // game loop
        while (true)
        {
            gameState.day = int.Parse(Console.ReadLine()!); // the game lasts 24 days: 0-23
            gameState.nutrients = int.Parse(Console.ReadLine()!); // the base score you gain from the next COMPLETE action
            inputs = Console.ReadLine()!.Split(' ');
            gameState.mySun = int.Parse(inputs[0]); // your sun points
            gameState.myScore = int.Parse(inputs[1]); // your current score
            inputs = Console.ReadLine()!.Split(' ');
            gameState.opponentSun = int.Parse(inputs[0]); // opponent's sun points
            gameState.opponentScore = int.Parse(inputs[1]); // opponent's score
            gameState.opponentIsWaiting = inputs[2] != "0"; // whether your opponent is asleep until the next day

            gameState.trees.Clear();
            var numberOfTrees = int.Parse(Console.ReadLine()!); // the current amount of trees
            for (var i = 0; i < numberOfTrees; i++)
            {
                inputs = Console.ReadLine()!.Split(' ');
                var cellIndex = int.Parse(inputs[0]); // location of this tree
                var size = int.Parse(inputs[1]); // size of this tree: 0-3
                var isMine = inputs[2] != "0"; // 1 if this is your tree
                var isDormant = inputs[3] != "0"; // 1 if this tree is dormant
                var tree = new Tree(cellIndex, size, isMine, isDormant);
                gameState.trees.Add(tree);
            }

            gameState.possibleActions.Clear();
            var numberOfPossibleMoves = int.Parse(Console.ReadLine()!);
            for (var i = 0; i < numberOfPossibleMoves; i++)
            {
                var possibleMove = Console.ReadLine()!;
                gameState.possibleActions.Add(Action.Parse(possibleMove));
            }

            var action = Solver.GetSolution(gameState, new Countdown(100));
            Console.WriteLine(action);
        }
    }
}

public class SunCalculator
{
    public static (int mySun, int oppSun) Calculate(GameState state)
    {
        var sunDir = state.day % 6;
        var board = state.board;
        var trees = state.trees;
        
        var treeAt = new Tree[board.Count];
        foreach (var t in trees)
            treeAt[t.cellIndex] = t;
        
        var spooky = new bool[board.Count];
        
        foreach (var source in trees.Where(t => t.size > 0))
        {
            var current = source.cellIndex;
            for (var step = 0; step < source.size; step++)
            {
                var next = board[current].neighbours[sunDir];
                if (next == -1) break;
                var targetTree = treeAt[next];
                if (source.size >= targetTree.size)
                {
                    spooky[next] = true;
                }
                current = next;
            }
        }

        int mySun = 0, oppSun = 0;
        foreach (var tree in trees)
        {
            if (tree.size == 0) continue;
            if (spooky[tree.cellIndex]) continue;
            if (tree.isMine) mySun += tree.size;
            else oppSun += tree.size;
        }
        
        return (mySun, oppSun);
    }
}

public class Tree
{
    public int cellIndex;
    public int size;
    public bool isMine;
    public bool isDormant;

    public Tree(int cellIndex, int size, bool isMine, bool isDormant)
    {
        this.cellIndex = cellIndex;
        this.size = size;
        this.isMine = isMine;
        this.isDormant = isDormant;
    }
    
    public Tree Clone() => new
    (
        cellIndex, 
        size, 
        isMine, 
        isDormant
    );
}
