using MelonLoader;
using UnityEngine;

namespace QoLFireStart;

public class Main : MelonMod
{
    public static FireStartManager FireStartManager;
    
    public override void OnInitializeMelon()
    {
        FireStartManager = new FireStartManager();
        Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");
    }
}