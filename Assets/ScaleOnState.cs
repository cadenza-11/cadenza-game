using DG.Tweening;
using UnityEngine;

public class ScaleOnState : StateMachineBehaviour
{
    public Vector3 scaleMultiplier = new Vector3(1.2f, 1.2f, 1f);
    public float duration = 0.2f;
    public int vibrato = 10;
    public float elasticity = 1;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Transform t = animator.transform;
        DOTween.Punch(
            getter: () => t.localScale,
            setter: value => t.localScale = value,
            direction: this.scaleMultiplier,
            duration: this.duration,
            vibrato: this.vibrato,
            elasticity: this.elasticity);
    }
}
