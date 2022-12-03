using System;
using UnityEngine;

namespace Mobge {

    [Serializable]
    [CreateAssetMenu(fileName = nameof(DataFloat), menuName = "Mobge/" + nameof(DataFloat))]
    public class DataFloat : Data<float> {}
}