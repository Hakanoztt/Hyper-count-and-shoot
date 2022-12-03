using System;
using UnityEngine;

namespace Mobge {

    [Serializable]
    [CreateAssetMenu(fileName = nameof(DataString), menuName = "Mobge/" + nameof(DataString))]
    public class DataString : Data<string> {}
}