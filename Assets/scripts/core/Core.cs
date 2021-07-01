using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Core
{
    public delegate void GameEvent(object sender, params object[] args);

    static Dictionary<string, GameEvent> _eventBag = new Dictionary<string, GameEvent>();

    public static void SubscribeEvent(string eventName, GameEvent gameEvent)
    {
        GameEvent existing;
        if (_eventBag.TryGetValue(eventName, out existing))
        {
            existing += gameEvent;
        }
        else
        {
            existing = gameEvent;
        }
        
        _eventBag[eventName] = existing;
    }

    public static void UnSubscribeEvent(string eventName, GameEvent gameEvent)
    {
        GameEvent existing;
        if (_eventBag.TryGetValue(eventName, out existing))
        {
            existing -= gameEvent;
        }
        
        if (existing == null)
        {
            _eventBag.Remove(eventName);
        }
        else
        {
            _eventBag[eventName] = existing;
        }
    }
    
    public static void BroadcastEvent(string eventName, object sender, params object[] args)
    {
        GameEvent existing;

        if (_eventBag.TryGetValue(eventName, out existing))
        {
            existing(sender, args);
        }
    }

    public static void ClearBag()
    {

    }
}
