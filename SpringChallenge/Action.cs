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