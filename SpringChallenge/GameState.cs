namespace SpringChallenge;

public class GameState
{
    public int day;
    public int nutrients;
    public int mySun, oppSun;
    public int myScore, oppScore;
    public List<Tree> trees = new List<Tree>();
    
    public static List<Cell> Board = new List<Cell>();
    public static int[,] Distances = new int[37, 37];

    public GameState Clone()
    {
        GameState clone = new GameState
        {
            day = this.day,
            nutrients = this.nutrients,
            mySun = this.mySun,
            oppSun = this.oppSun,
            myScore = this.myScore,
            oppScore = this.oppScore,
            trees = new List<Tree>(this.trees.Count)
        };
        foreach (var t in this.trees) clone.trees.Add(t.Clone());
        return clone;
    }

    // Применяем действие к текущему состоянию
    public void ApplyAction(Action action)
    {
        if (action.type == Action.WAIT) return;

        if (action.type == Action.GROW)
        {
            var tree = trees.First(t => t.cellIndex == action.targetCellIdx);
            tree.size++;
            tree.isDormant = true;
        }
        else if (action.type == Action.SEED)
        {
            var source = trees.First(t => t.cellIndex == action.sourceCellIdx);
            source.isDormant = true;
            trees.Add(new Tree(action.targetCellIdx, 0, true, true));
        }
        else if (action.type == Action.COMPLETE)
        {
            var tree = trees.First(t => t.cellIndex == action.targetCellIdx);
            trees.Remove(tree);
            myScore += nutrients + GetBonus(Board[action.targetCellIdx].richness);
            nutrients = Math.Max(0, nutrients - 1);
        }
    }

    private int GetBonus(int richness) => richness == 3 ? 4 : richness == 2 ? 2 : 0;

    // Эмуляция конца дня (самая важная часть: тени и сбор солнца)
    public void EndDay()
    {
        int sunDir = day % 6;
        int[] shadowLevel = new int[37];

        // 1. Просчитываем тени
        foreach (var tree in trees)
        {
            int currentCell = tree.cellIndex;
            for (int i = 0; i < tree.size; i++)
            {
                currentCell = Board[currentCell].neighbours[sunDir];
                if (currentCell == -1) break;
                shadowLevel[currentCell] = Math.Max(shadowLevel[currentCell], tree.size);
            }
        }

        // 2. Собираем солнце
        foreach (var tree in trees)
        {
            if (shadowLevel[tree.cellIndex] < tree.size)
            {
                if (tree.isMine) mySun += tree.size;
                else oppSun += tree.size;
            }
            tree.isDormant = false; // Пробуждаем деревья
        }

        day++;
    }

    // Оценка позиции в конце симуляции (эвристика)
    public double Evaluate()
    {
        double score = myScore - oppScore;
        // Бонус за размер деревьев на поле (чтобы бот хотел расти)
        foreach (var tree in trees)
        {
            if (tree.isMine) score += tree.size * 2 + (Board[tree.cellIndex].richness);
            else score -= tree.size * 2 + (Board[tree.cellIndex].richness);
        }
        // Бонус за накопленное солнце
        score += (mySun / 3.0) - (oppSun / 3.0);
        return score;
    }
}