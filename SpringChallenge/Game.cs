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
        // Если единственное доступное действие - WAIT
        if (possibleActions is [{ type: Action.WAIT }])
        {
            return possibleActions[0];
        }

        // 1. COMPLETE (Завершение цикла)
        // Завершаем, если дерево стоит на хорошей почве (richness)
        var completes = possibleActions.Where(a => a.type == Action.COMPLETE).ToList();
        if (completes.Count != 0)
        {
            // Выбираем дерево на самой богатой клетке для получения бонуса
            var bestComplete = completes.OrderByDescending(a => GetRichness(a.targetCellIdx)).First();
            return bestComplete;
        }

        // 2. GROW (Рост)
        var grows = possibleActions.Where(a => a.type == Action.GROW).ToList();
        if (grows.Count != 0)
        {
            // Стратегия: предпочитаем растить деревья, которые уже большие (size 2 -> 3), 
            // так как они приносят больше солнца и готовятся к COMPLETE.
            // Для этого найдем размер дерева, которое собираемся растить.
            var bestGrow = grows.OrderByDescending(a => 
            {
                var tree = trees.First(t => t.cellIndex == a.targetCellIdx);
                return tree.size; // Чем больше текущий размер, тем выше приоритет
            }).First();
            
            return bestGrow;
        }

        // 3. SEED (Посадка семян)
        var seeds = possibleActions.Where(a => a.type == Action.SEED).ToList();
        if (seeds.Count != 0)
        {
            // Ограничиваем количество семян, чтобы не тратить все солнце на малышей
            int mySeedsCount = trees.Count(t => t is { isMine: true, size: 0 });
            int myTreesCount = trees.Count(t => t.isMine);
            
            if (mySeedsCount < 2 && myTreesCount < 6) 
            {
                // Кидаем семечко на самую богатую почву
                var bestSeed = seeds.OrderByDescending(a => GetRichness(a.targetCellIdx)).First();
                return bestSeed;
            }
        }

        // 4. WAIT (Конец хода)
        // Если не хотим тратить очки или нет выгодных действий
        return possibleActions.First(a => a.type == Action.WAIT);
    }
    
    private int GetRichness(int cellIndex) => board.First(c => c.index == cellIndex).richness;
}