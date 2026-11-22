using TMPro;
using UnityEngine;
#if !UNITY_EDITOR
using KmyTarkovUtils;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;

#endif

namespace GamePanelHUDGrenade.Views
{
    public class GrenadeUIView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        public bool zeroWarning;

        public int grenadeAmount;

        public Color grenadeColor;

        public Color warningColor;

        public FontStyles grenadeStyles;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        // ReSharper disable once InconsistentNaming
        [SerializeField] private TMP_Text countValue;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

#if !UNITY_EDITOR

        private Changed<bool> _cZeroWarning;
        private Changed<int> _cGrenadeAmount;
        private Changed<Color> _cGrenadeColor, _cWarningColor;
        private Changed<FontStyles> _cGrenadeStyles;

        /// <summary>
        /// True when any input differs from the previous frame. Uses non-short-circuiting | so EVERY
        /// TrySet runs and updates its snapshot -- with || the later comparisons would be skipped on the
        /// first change and their snapshots would go stale, silently freezing those values on screen.
        /// </summary>
        private bool InputsChanged()
        {
            return _cZeroWarning.TrySet(zeroWarning)
                 | _cGrenadeAmount.TrySet(grenadeAmount)
                 | _cGrenadeColor.TrySet(grenadeColor)
                 | _cWarningColor.TrySet(warningColor)
                 | _cGrenadeStyles.TrySet(grenadeStyles);
        }

        private void ResetGate()
        {
            _cZeroWarning.Reset();
            _cGrenadeAmount.Reset();
            _cGrenadeColor.Reset();
            _cWarningColor.Reset();
            _cGrenadeStyles.Reset();
        }

        private void Start()
        {
            HUDCoreModel.Instance.UpdateManger.Register(this);
        }

        private void OnEnable()
        {
            ResetGate();
            HUDCoreModel.Instance.UpdateManger.Run(this);
        }

        private void OnDisable()
        {
            HUDCoreModel.Instance.UpdateManger.Stop(this);
        }

        public void CustomUpdate()
        {
            if (!InputsChanged())
            {
                return;
            }

            //Set GrenadeAmount int and color and Style to String
            Color grenadeValueColor;
            if (zeroWarning && grenadeAmount != 0 || !zeroWarning)
            {
                grenadeValueColor = grenadeColor;
            }
            else
            {
                grenadeValueColor = warningColor;
            }

            countValue.fontStyle = grenadeStyles;
            countValue.color = grenadeValueColor;
            countValue.text = grenadeAmount.ToString();
        }

#endif
    }
}