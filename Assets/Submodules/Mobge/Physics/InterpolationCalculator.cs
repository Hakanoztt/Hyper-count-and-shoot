using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge {
    public struct InterpolationCalculator {
        float _time;


        public void Update(out float currentRate) {
            _time += Time.deltaTime;
            _time = Mathf.Min(_time, Time.maximumDeltaTime);
            float fdt = Time.fixedDeltaTime;
            float division = Mathf.Floor(_time / fdt);
            _time -= division * fdt;
            currentRate = _time / fdt;
            //Debug.Log(Time.deltaTime + " : " + fdt + " : " + currentRate);

        }
    }
}