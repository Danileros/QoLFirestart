using MelonLoader;
using UnityEngine;

namespace QoLFireStart;

public class QoLFireStartMain : MelonMod
{
    public override void OnInitializeMelon()
    {
        Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");
    }
}