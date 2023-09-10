using Fusion;

public struct SecondTicker : INetworkStruct
{
    public int secondsElapsed;
    public bool secondHasTicked;

    private int _initialTick;

    // store a reference for easy calcualtion
    private int _tickRate;

    public static SecondTicker CreateSecondTicker => new SecondTicker();

    public void Initialize(NetworkRunner runner)
    {
        _initialTick = runner.Simulation.Tick;
        _tickRate = runner.Simulation.Config.TickRate;
    }

    /// <summary>
    /// Needs to run in FixedNetworkUpdate to calculate the seconds elapsed
    /// </summary>
    /// <param name="runner"></param>
    public void Tick(NetworkRunner runner)
    {
        if (runner == false || runner.IsRunning == false)
            return;

        int elapsedTicks = runner.Simulation.Tick - _initialTick;
        int calcualtedSeconds = elapsedTicks / _tickRate;
        if (calcualtedSeconds == secondsElapsed)
        {
            secondHasTicked = false;
            return;
        }

        if (calcualtedSeconds > secondsElapsed)
        {
            secondHasTicked = true;
            secondsElapsed = calcualtedSeconds;
            //Debug.Log($"[SecondTicker] second has elapsed, seconds elapsed {secondsElapsed}, calculated seconds {calcualtedSeconds}");
        }
    }
}