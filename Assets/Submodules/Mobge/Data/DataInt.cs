using System;
using UnityEngine;

namespace Mobge {

    [Serializable]
    [CreateAssetMenu(fileName = nameof(DataInt), menuName = "Mobge/" + nameof(DataInt))]
    public class DataInt : Data<int> {}
}
