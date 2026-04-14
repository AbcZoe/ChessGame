using UnityEngine;

public static class Services
{
    public static NetInterface NetInterface { get; private set; }

    public static void SetNetInterface(NetInterface impl)
    {
        NetInterface = impl;
    }

}
