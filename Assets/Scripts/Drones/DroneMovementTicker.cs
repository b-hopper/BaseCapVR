using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

public struct DroneMovementTicker : INetworkStruct
{
    public bool moveHasTicked;
    
    public int moveTicksElapsed;

    private int _initialTick;
    private float _droneMoveInterval => GameSettingsManager.Instance.droneSendInterval;


    public static DroneMovementTicker CreateSecondTicker => new DroneMovementTicker();

    public void Initialize(NetworkRunner runner)
    {
        _initialTick = runner.Simulation.Tick;
    }
    
    int _prevTicksPerMove;

    /// <summary>
    /// Needs to run in FixedNetworkUpdate to calculate the seconds elapsed
    /// </summary>
    /// <param name="runner"></param>
    public void Tick(NetworkRunner runner)
    {
        if (runner == false || runner.IsRunning == false)
            return;

        int elapsedTicks = runner.Simulation.Tick - _initialTick;
        int ticksPerMove = (int)(runner.Simulation.Config.TickRate * _droneMoveInterval);

        int elapsed = elapsedTicks / ticksPerMove;
        
        if (_prevTicksPerMove != ticksPerMove)
        {
            moveTicksElapsed = elapsed;
            _prevTicksPerMove = ticksPerMove;
        }
        
        if (elapsed > moveTicksElapsed)
        {
            moveHasTicked = true;
            moveTicksElapsed = elapsed;
            return;
        }
        moveHasTicked = false;
        
    }
}