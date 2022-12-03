using UnityEngine;

namespace Mobge.Animation {
    public class AnimatorStateAttribute : PropertyAttribute {
        public string noneOption;

        /// <summary>
        /// Turns field editor into animator state drop down. Works for integers and strings. For integer mode sets value of field to <see cref="AnimatorStateInfo.shortNameHash"/> of selected state.
        /// </summary>
        /// <param name="noneOption"></param>
        public AnimatorStateAttribute(string noneOption = "none") {
            this.noneOption = noneOption;
        }
    }
    public class AnimatorFloatParameterAttribute : AnimatorStateAttribute { }
    public class AnimatorBoolParameterAttribute : AnimatorStateAttribute { }
    public class AnimatorIntParameterAttribute : AnimatorStateAttribute { }
    public class AnimatorTriggerAttribute : AnimatorStateAttribute { }
    public interface IAnimatorOwner {
        Animator GetAnimator();
    }
    public interface IAnimatorControllerOwner {
        RuntimeAnimatorController GetAnimatorController();
    }
}