using MelonLoader;
using UnityEngine;

namespace QoLFireStart;

public class Main : MelonMod
{
    public override void OnInitializeMelon()
    {
        Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");
    }
}