using TMPro;
using UnityEngine;
#if !UNITY_EDITOR
using SettingsModel = GamePanelHUDGrenade.Models.SettingsModel;
using KmyTarkovUtils;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;
using GamePanelHUDGrenade.Models;

#endif

namespace GamePanelHUDGrenade.Views
{
    public class GrenadeHUDView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        [SerializeField] private GrenadeUIView fragUIView;

        [SerializeField] private GrenadeUIView stunUIView;

        [SerializeField] private GrenadeUIView smokeUIView;

        private RectTransform _rectTransform;

#if !UNITY_EDITOR

        private Changed<Vector2> _cAnchoredPosition, _cSizeDelta, _cLocalScale;
        private Changed<bool> _cGrenadeHUDSw, _cMergeGrenade, _cZeroWarning;
        private Changed<int> _cFragAmount, _cStunAmount, _cSmokeAmount;
        private Changed<Color> _cFragColor, _cStunColor, _cSmokeColor, _cWarningColor;
        private Changed<FontStyles> _cFragStyles, _cStunStyles, _cSmokeStyles;

        /// <summary>
        /// True when any input differs from the previous frame. Uses non-short-circuiting | so EVERY
        /// TrySet runs and updates its snapshot -- with || the later comparisons would be skipped on the
        /// first change and their snapshots would go stale, silently freezing those values on screen.
        /// </summary>
        private bool InputsChanged(SettingsModel settingsModel, GrenadeHUDModel grenadeHUDModel)
        {
            return _cAnchoredPosition.TrySet(settingsModel.KeyAnchoredPosition.Value)
                 | _cSizeDelta.TrySet(settingsModel.KeySizeDelta.Value)
                 | _cLocalScale.TrySet(settingsModel.KeyLocalScale.Value)
                 | _cGrenadeHUDSw.TrySet(grenadeHUDModel.GrenadeHUDSw)
                 | _cMergeGrenade.TrySet(settingsModel.KeyMergeGrenade.Value)
                 | _cZeroWarning.TrySet(settingsModel.KeyZeroWarning.Value)
                 | _cFragAmount.TrySet(grenadeHUDModel.AllAmount.Frag)
                 | _cStunAmount.TrySet(grenadeHUDModel.AllAmount.Stun + grenadeHUDModel.AllAmount.Flash)
                 | _cSmokeAmount.TrySet(grenadeHUDModel.AllAmount.Smoke)
                 | _cFragColor.TrySet(settingsModel.KeyFragColor.Value)
                 | _cStunColor.TrySet(settingsModel.KeyStunColor.Value)
                 | _cSmokeColor.TrySet(settingsModel.KeySmokeColor.Value)
                 | _cWarningColor.TrySet(settingsModel.KeyWarningColor.Value)
                 | _cFragStyles.TrySet(settingsModel.KeyFragStyles.Value)
                 | _cStunStyles.TrySet(settingsModel.KeyStunStyles.Value)
                 | _cSmokeStyles.TrySet(settingsModel.KeySmokeStyles.Value);
        }

        private void ResetGate()
        {
            _cAnchoredPosition.Reset(); _cSizeDelta.Reset(); _cLocalScale.Reset();
            _cGrenadeHUDSw.Reset(); _cMergeGrenade.Reset(); _cZeroWarning.Reset();
            _cFragAmount.Reset(); _cStunAmount.Reset(); _cSmokeAmount.Reset();
            _cFragColor.Reset(); _cStunColor.Reset(); _cSmokeColor.Reset(); _cWarningColor.Reset();
            _cFragStyles.Reset(); _cStunStyles.Reset(); _cSmokeStyles.Reset();
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
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
            var settingsModel = SettingsModel.Instance;
            var grenadeHUDModel = GrenadeHUDModel.Instance;

            if (!InputsChanged(settingsModel, grenadeHUDModel))
            {
                return;
            }

            //Set RectTransform anchoredPosition and localScale
            _rectTransform.anchoredPosition = settingsModel.KeyAnchoredPosition.Value;
            _rectTransform.sizeDelta = settingsModel.KeySizeDelta.Value;
            _rectTransform.localScale = settingsModel.KeyLocalScale.Value;

            #region fragView

            fragUIView.gameObject.SetActive(GrenadeHUDModel.Instance.GrenadeHUDSw);
            fragUIView.zeroWarning = settingsModel.KeyZeroWarning.Value;

            fragUIView.grenadeAmount = grenadeHUDModel.AllAmount.Frag;

            fragUIView.grenadeColor = settingsModel.KeyFragColor.Value;
            fragUIView.warningColor = settingsModel.KeyWarningColor.Value;

            fragUIView.grenadeStyles = settingsModel.KeyFragStyles.Value;

            #endregion

            #region stunView

            stunUIView.gameObject.SetActive(grenadeHUDModel.GrenadeHUDSw && !settingsModel.KeyMergeGrenade.Value);
            stunUIView.zeroWarning = settingsModel.KeyZeroWarning.Value;

            stunUIView.grenadeAmount = grenadeHUDModel.AllAmount.Stun + grenadeHUDModel.AllAmount.Flash;

            stunUIView.grenadeColor = settingsModel.KeyStunColor.Value;
            stunUIView.warningColor = settingsModel.KeyWarningColor.Value;

            stunUIView.grenadeStyles = settingsModel.KeyStunStyles.Value;

            #endregion

            #region smokeView

            smokeUIView.gameObject.SetActive(grenadeHUDModel.GrenadeHUDSw && !settingsModel.KeyMergeGrenade.Value);
            smokeUIView.zeroWarning = settingsModel.KeyZeroWarning.Value;

            smokeUIView.grenadeAmount = grenadeHUDModel.AllAmount.Smoke;

            smokeUIView.grenadeColor = settingsModel.KeySmokeColor.Value;
            smokeUIView.warningColor = settingsModel.KeyWarningColor.Value;

            smokeUIView.grenadeStyles = settingsModel.KeySmokeStyles.Value;

            #endregion
        }

#endif
    }
}