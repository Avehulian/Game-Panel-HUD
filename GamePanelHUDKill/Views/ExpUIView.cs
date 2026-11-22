using TMPro;
using UnityEngine;
#if !UNITY_EDITOR
using KmyTarkovUtils;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;

#endif

namespace GamePanelHUDKill.Views
{
    public class ExpUIView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        public bool active;

        public int xp;

        public int waitXp;

        public Color xpColor;

        public float xpWaitSpeed;

        public FontStyles xpStyles;

#if !UNITY_EDITOR

        public bool IsAnim => !IsPassive && !IsClear;

        private bool IsPassive => _animatorExpUI.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimatorHash.Passive;

        private bool IsClear => _animatorExpUI.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimatorHash.Clear;

#endif

        [SerializeField] private TMP_Text xpValue;

        private Animator _animatorExpUI;

#if !UNITY_EDITOR

        // Only xpWaitSpeed is gated here. The rest of this method depends on IsClear/IsPassive, which
        // read the Animator's own current-state hash -- that hash advances on the Animator's internal
        // clock, not from any field on this view, so it is not something a Changed<T> snapshot can see.
        // Wrapping the whole method in one outer gate would risk missing a frame where `active` hasn't
        // changed but IsClear just flipped, silently freezing the XP counter. SetFloat is called every
        // frame regardless of that state machine, so it is the one part safe to gate on its own.
        private Changed<float> _cXpWaitSpeed;

        private void ResetGate()
        {
            _cXpWaitSpeed.Reset();
        }

        private void Awake()
        {
            _animatorExpUI = GetComponent<Animator>();
        }

        private void Start()
        {
            HUDCoreModel.Instance.UpdateManger.Register(this);
        }

        private void OnEnable()
        {
            ResetGate();
        }

        public void CustomUpdate()
        {
            if (_cXpWaitSpeed.TrySet(xpWaitSpeed))
            {
                _animatorExpUI.SetFloat(AnimatorHash.Speed, xpWaitSpeed);
            }

            if (!active || IsClear)
                return;

            xp += waitXp;

            waitXp = 0;

            xpValue.fontStyle = xpStyles;
            xpValue.color = xpColor;
            xpValue.text = xp.ToString();

            _animatorExpUI.SetTrigger(AnimatorHash.Active);

            active = false;
        }

        public void XpUp(int up, int lastXp)
        {
            if (!IsClear)
            {
                xp += up + lastXp;

                active = true;
            }
            else
            {
                waitXp += up + lastXp;

                active = true;
            }
        }

        public void XpComplete()
        {
            _animatorExpUI.SetTrigger(AnimatorHash.Complete);
        }

#endif

        // ReSharper disable once UnusedMember.Local
        private void ClearXp()
        {
            xp = 0;
        }
    }
}