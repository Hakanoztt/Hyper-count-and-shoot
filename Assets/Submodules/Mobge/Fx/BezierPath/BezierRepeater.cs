using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Mobge.BezierRepeaterComponent;

namespace Mobge {
    public class BezierRepeater : MonoBehaviour {


        public Repeater repeater = new Repeater() {
            sampleStep = new Step() { type = StepType.Percentage, value = 0.1f },
            stepPerSpawn = 1,
        };


    }
}