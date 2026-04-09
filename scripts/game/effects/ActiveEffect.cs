public class ActiveEffect
{
    public EffectData Data;
    public float RemainingDuration;
    public float TimeSinceLastTick;

    public ActiveEffect(EffectData data)
    {
        Data = data;
        RemainingDuration = data.Duration;
        TimeSinceLastTick = 0f;
    }
}
