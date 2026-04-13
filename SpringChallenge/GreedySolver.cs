namespace SpringChallenge;

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
            
            if (bestAction.type == Action.Wait)
            {
                NextDay(current);
            }
        }

        return current;
    }
    
    private void NextDay(GameState state)
    {
        state.day++;
        
        var (mySun, oppSun) = SunCalculator.Calculate(state);
        state.mySun += mySun;
        state.opponentSun += oppSun;
        
        foreach (var t in state.trees)
            t.isDormant = false;
    }
}