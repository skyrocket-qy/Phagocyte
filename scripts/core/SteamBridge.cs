using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Steamworks SDK bridge — reserved integration point (docs/achievement.md §3).
/// The actual SDK calls compile only when the <c>USE_STEAMWORKS</c> symbol is defined
/// (add Steamworks.NET / GodotSteam and enable the symbol in Phagocyte.csproj).
/// Without the symbol every method is a safe no-op so the game remains fully
/// playable offline, with user://achievements.json as the source of truth.
/// </summary>
public static class SteamBridge
{
    /// <summary>True only when the Steamworks SDK is compiled in and the client is initialized.</summary>
    public static bool IsAvailable { get; private set; }

    public static void Initialize()
    {
        IsAvailable = false;
#if USE_STEAMWORKS
        try
        {
            if (SteamManager.IsInitialized)
            {
                SteamUserStats.RequestCurrentStats();
                IsAvailable = true;
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SteamBridge: initialization failed ({ex.Message}). Running offline.");
            IsAvailable = false;
        }
#endif
    }

    public static void Shutdown()
    {
        IsAvailable = false;
        // SteamAPI.Shutdown() is owned by the SteamManager autoload, not by this bridge.
    }

    /// <summary>
    /// Broadcast one locally unlocked achievement. Also used as the offline
    /// catch-up batch on the next online launch.
    /// </summary>
    public static void PushUnlock(string achId)
    {
#if USE_STEAMWORKS
        if (!IsAvailable || string.IsNullOrEmpty(achId))
            return;

        try
        {
            SteamUserStats.SetAchievement(GetSteamApiName(achId));
            SteamUserStats.StoreStats();
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SteamBridge: failed to push '{achId}' ({ex.Message}).");
        }
#endif
    }

    /// <summary>Batch form of <see cref="PushUnlock"/> for the offline catch-up pass.</summary>
    public static void PushUnlocks(IEnumerable<string> achIds)
    {
        if (achIds == null)
            return;

        foreach (string achId in achIds)
            PushUnlock(achId);
    }

    /// <summary>
    /// Pulls the subset of <paramref name="candidateIds"/> that Steam already has
    /// unlocked, enabling two-way sync onto a fresh local profile.
    /// </summary>
    public static List<string> PullUnlocked(IEnumerable<string> candidateIds)
    {
        var remoteUnlocked = new List<string>();
#if USE_STEAMWORKS
        if (!IsAvailable || candidateIds == null)
            return remoteUnlocked;

        try
        {
            foreach (string achId in candidateIds)
            {
                if (string.IsNullOrEmpty(achId))
                    continue;

                if (SteamUserStats.GetAchievement(GetSteamApiName(achId), out bool unlocked) && unlocked)
                    remoteUnlocked.Add(achId);
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SteamBridge: failed to pull remote unlocks ({ex.Message}).");
        }
#endif
        return remoteUnlocked;
    }

    /// <summary>Local ids already mirror the Steam API naming; only the case needs normalizing.</summary>
    public static string GetSteamApiName(string achId)
    {
        return string.IsNullOrEmpty(achId) ? "" : achId.ToUpperInvariant();
    }
}
