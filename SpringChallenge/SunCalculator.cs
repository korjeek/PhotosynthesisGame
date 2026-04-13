namespace SpringChallenge;

public class SunCalculator
{
    public static (int mySun, int oppSun) Calculate(GameState state)
    {
        int sunDir = state.day % 6;
        var board = state.board;
        var trees = state.trees;

        // Для каждой клетки список деревьев, отбрасывающих на неё тень
        var shadowSources = new List<Tree>[board.Count];
        for (int i = 0; i < board.Count; i++)
            shadowSources[i] = new List<Tree>();

        // Для каждого дерева, отбрасывающего тень
        foreach (var tree in trees.Where(t => t.size > 0))
        {
            int current = tree.cellIndex;
            for (int step = 0; step < tree.size; step++)
            {
                int next = board[current].neighbours[sunDir];
                if (next == -1) break;
                shadowSources[next].Add(tree);
                current = next;
            }
        }

        // Для каждой клетки определяем, есть ли на ней дерево и затенено ли оно
        var treeAt = new Tree[board.Count];
        foreach (var t in trees)
            treeAt[t.cellIndex] = t;

        bool[] spooky = new bool[board.Count];
        for (int i = 0; i < board.Count; i++)
        {
            var targetTree = treeAt[i];
            if (targetTree == null) 
                continue;
            // Проверяем все деревья, отбрасывающие тень на эту клетку
            foreach (var source in shadowSources[i])
            {
                if (source.size >= targetTree.size)
                {
                    spooky[i] = true;
                    break;
                }
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