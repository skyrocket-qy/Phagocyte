namespace Phagocyte.Core;

/// <summary>
/// Typed contract for universal-stat hosts (CellStats).
/// Replaces string duck-typing (<c>HasMethod("get_stat")</c>).
/// </summary>
public interface IStatHost
{
    float GetStat(string statName);
    void AddModifier(string statName, float flat, float percent);
    void RemoveModifier(string statName, float flat, float percent);
    void AddScaledModifier(string target, float flat, float percent, string scalingStat, float scalePer);
    void RemoveScaledModifier(string target, float flat, float percent, string scalingStat, float scalePer);
}
