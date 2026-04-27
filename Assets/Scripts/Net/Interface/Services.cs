using UnityEngine;

public static class Services
{
    public static NetInterface NetInterface { get; private set; }

    public static bool IsInitialized => NetInterface != null;

    public static void SetNetInterface(NetInterface impl)
    {
        if (impl == null)
        {
            Debug.LogError("NetInterface cannot be null!");
            return;
        }

        NetInterface = impl;
    }

    public static void Clear()
    {
        NetInterface = null;
    }
}