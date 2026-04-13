using System.Diagnostics;

namespace SpringChallenge;

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