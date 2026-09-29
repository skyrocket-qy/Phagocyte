namespace Game.Core;

public interface IStatHost
{
    float GetStat(string statName);
    bool HasStat(string statName);
    void AddModifier(string statName, float flat, float percent);
    void RemoveModifier(string statName, float flat, float percent);
    void AddScaledModifier(string target, float flat, float percent, string scalingStat, float scalePer);
    bool RemoveScaledModifier(string target, float flat, float percent, string scalingStat, float scalePer);
}
