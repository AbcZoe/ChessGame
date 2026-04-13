using UnityEngine;

public static class Services
{
    public static NetInterface NetGame { get; private set; }
    public static void SetNetGame(NetInterface impl)
    {
        NetGame = impl;
    }

}
