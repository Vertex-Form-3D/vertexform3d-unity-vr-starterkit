using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Somewhere an app can get the session list from other than the Photon lobby runner.
///
/// The lobby runner costs one extra CCU per player. An app that wants every CCU for players (the
/// "20 CCU" setting) has no lobby, so no list. This lets it plug in its own source — for example a
/// web API fed by the clients — without the framework knowing anything about it.
///
/// Nothing registers by default, so with no source the framework behaves exactly as before.
/// The source itself belongs in the app, outside the framework folder, so a package update never
/// overwrites it.
/// </summary>
public interface ISessionDirectorySource
{
    IReadOnlyList<SessionInfoData> Sessions { get; }

    /// <summary>What to show after the slash in "3/20".</summary>
    int DefaultMaxPlayers { get; }
}

/// <summary>
/// The one place the UI reads "which rooms exist and how full are they" from.
/// An external source wins when one is registered; otherwise it is the lobby runner's list.
/// </summary>
public static class SessionDirectory
{
    static ISessionDirectorySource _external;
    static readonly List<SessionInfoData> Empty = new List<SessionInfoData>();

    /// <summary>Raised when the list changes, from either source. Subscribers must detach in OnDisable/OnDestroy.</summary>
    public static event Action Changed;

    public static bool HasExternalSource => _external != null;

    /// <summary>True when there is any source of live counts: the lobby runner or an external one.</summary>
    public static bool ShowsPlayerCounts => HasExternalSource || ProjectManager.UsesPhotonSessionLobbyRunner;

    /// <summary>The number after the slash in "3/20". Stays 10 for the lobby runner, as it always was.</summary>
    public static int DefaultMaxPlayers =>
        _external != null && _external.DefaultMaxPlayers > 0 ? _external.DefaultMaxPlayers : 10;

    public static IReadOnlyList<SessionInfoData> Sessions
    {
        get
        {
            if (_external != null)
                return _external.Sessions ?? Empty;

            NetworkLobby lobby = NetworkLobby.Instance;
            return lobby != null ? lobby.Sessions : Empty;
        }
    }

    public static void RegisterExternalSource(ISessionDirectorySource source)
    {
        _external = source;
        RaiseChanged();
    }

    public static void UnregisterExternalSource(ISessionDirectorySource source)
    {
        if (_external != source)
            return;

        _external = null;
        RaiseChanged();
    }

    public static void RaiseChanged()
    {
        try
        {
            Changed?.Invoke();
        }
        catch (Exception e)
        {
            // One broken subscriber must not stop the others hearing about the change.
            Debug.LogError($"[SessionDirectory] A Changed subscriber threw: {e}");
        }
    }
}
