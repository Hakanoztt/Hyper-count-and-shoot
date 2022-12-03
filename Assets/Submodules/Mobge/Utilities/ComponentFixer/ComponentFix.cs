using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ComponentFix : ScriptableObject {
    public abstract void ShowFixables();
    public abstract void DoFix();
}
