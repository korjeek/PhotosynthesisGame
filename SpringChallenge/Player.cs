namespace SpringChallenge;

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