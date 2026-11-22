using TMPro;
using UnityEngine;
#if !UNITY_EDITOR
using KmyTarkovUtils;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;
using GamePanelHUDHealth.Models;
using SettingsModel = GamePanelHUDHealth.Models.SettingsModel;

#endif

namespace GamePanelHUDHealth.Views
{
    public class HealthHUDView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        [SerializeField] private HealthUIView overallHealth;

        //[SerializeField] private GamePanelHUDHealthUI poisoning;

        //[SerializeField] private GamePanelHUDHealthUI radiation;

        //[SerializeField] private GamePanelHUDHealthUI bloodPressure;

        [SerializeField] private HealthUIView energy;

        [SerializeField] private HealthUIView hydration;

        //[SerializeField] private GamePanelHUDHealthUI temperature;

        //[SerializeField] private GamePanelHUDHealthUI weight;

        private RectTransform _rectTransform;

#if !UNITY_EDITOR

        private Changed<Vector2> _cAnchoredPosition, _cSizeDelta, _cLocalScale;
        private Changed<bool> _cHealthHUDSw, _cBuffSw, _cArrowAnimation, _cArrowAnimationReverse,
                              _cHealthAtMaximum, _cHydrationAtMaximum, _cEnergyAtMaximum;
        private Changed<float> _cBuffSpeed,
                               _cHealthCurrent, _cHealthMaximum, _cHealthRate,
                               _cHydrationCurrent, _cHydrationMaximum, _cHydrationRate, _cHydrationNormalized,
                               _cEnergyCurrent, _cEnergyMaximum, _cEnergyRate, _cEnergyNormalized;
        private Changed<int> _cHealthWarningRate, _cHydrationWarningRate, _cEnergyWarningRate;
        private Changed<Color> _cUpBuffArrowColor, _cDownBuffArrowColor, _cMaxColor, _cAddZerosColor,
                               _cWarningColor, _cUpBuffColor, _cDownBuffColor,
                               _cHealthColor, _cHydrationColor, _cEnergyColor;
        private Changed<FontStyles> _cCurrentStyles, _cMaximumStyles;

        /// <summary>
        /// True when any input differs from the previous frame. Uses non-short-circuiting | so EVERY
        /// TrySet runs and updates its snapshot -- with || the later comparisons would be skipped on the
        /// first change and their snapshots would go stale, silently freezing those values on screen.
        /// </summary>
        private bool InputsChanged(HealthHUDModel healthHUDModel, SettingsModel settingsModel)
        {
            var health = healthHUDModel.Health;

            return _cAnchoredPosition.TrySet(settingsModel.KeyAnchoredPosition.Value)
                 | _cSizeDelta.TrySet(settingsModel.KeySizeDelta.Value)
                 | _cLocalScale.TrySet(settingsModel.KeyLocalScale.Value)
                 | _cHealthHUDSw.TrySet(healthHUDModel.HealthHUDSw)
                 | _cBuffSw.TrySet(settingsModel.KeyBuffSw.Value)
                 | _cArrowAnimation.TrySet(settingsModel.KeyArrowAnimation.Value)
                 | _cArrowAnimationReverse.TrySet(settingsModel.KeyArrowAnimationReverse.Value)
                 | _cBuffSpeed.TrySet(settingsModel.KeyBuffSpeed.Value)
                 | _cUpBuffArrowColor.TrySet(settingsModel.KeyUpBuffArrowColor.Value)
                 | _cDownBuffArrowColor.TrySet(settingsModel.KeyDownBuffArrowColor.Value)
                 | _cMaxColor.TrySet(settingsModel.KeyMaxColor.Value)
                 | _cAddZerosColor.TrySet(settingsModel.KeyAddZerosColor.Value)
                 | _cWarningColor.TrySet(settingsModel.KeyWarningColor.Value)
                 | _cUpBuffColor.TrySet(settingsModel.KeyUpBuffColor.Value)
                 | _cDownBuffColor.TrySet(settingsModel.KeyDownBuffColor.Value)
                 | _cCurrentStyles.TrySet(settingsModel.KeyCurrentStyles.Value)
                 | _cMaximumStyles.TrySet(settingsModel.KeyMaximumStyles.Value)
                 | _cHealthAtMaximum.TrySet(health.Common.AtMaximum)
                 | _cHealthCurrent.TrySet(health.Common.Current)
                 | _cHealthMaximum.TrySet(health.Common.Maximum)
                 | _cHealthRate.TrySet(health.HealthRate)
                 | _cHealthWarningRate.TrySet(settingsModel.KeyHealthWarningRate.Value)
                 | _cHealthColor.TrySet(settingsModel.KeyHealthColor.Value)
                 | _cHydrationAtMaximum.TrySet(health.Hydration.AtMaximum)
                 | _cHydrationCurrent.TrySet(health.Hydration.Current)
                 | _cHydrationMaximum.TrySet(health.Hydration.Maximum)
                 | _cHydrationRate.TrySet(health.HydrationRate)
                 | _cHydrationNormalized.TrySet(health.Hydration.Normalized)
                 | _cHydrationWarningRate.TrySet(settingsModel.KeyHydrationWarningRate.Value)
                 | _cHydrationColor.TrySet(settingsModel.KeyHydrationColor.Value)
                 | _cEnergyAtMaximum.TrySet(health.Energy.AtMaximum)
                 | _cEnergyCurrent.TrySet(health.Energy.Current)
                 | _cEnergyMaximum.TrySet(health.Energy.Maximum)
                 | _cEnergyRate.TrySet(health.EnergyRate)
                 | _cEnergyNormalized.TrySet(health.Energy.Normalized)
                 | _cEnergyWarningRate.TrySet(settingsModel.KeyEnergyWarningRate.Value)
                 | _cEnergyColor.TrySet(settingsModel.KeyEnergyColor.Value);
        }

        private void ResetGate()
        {
            _cAnchoredPosition.Reset(); _cSizeDelta.Reset(); _cLocalScale.Reset();
            _cHealthHUDSw.Reset(); _cBuffSw.Reset(); _cArrowAnimation.Reset(); _cArrowAnimationReverse.Reset();
            _cBuffSpeed.Reset();
            _cUpBuffArrowColor.Reset(); _cDownBuffArrowColor.Reset(); _cMaxColor.Reset();
            _cAddZerosColor.Reset(); _cWarningColor.Reset(); _cUpBuffColor.Reset(); _cDownBuffColor.Reset();
            _cCurrentStyles.Reset(); _cMaximumStyles.Reset();
            _cHealthAtMaximum.Reset(); _cHealthCurrent.Reset(); _cHealthMaximum.Reset(); _cHealthRate.Reset();
            _cHealthWarningRate.Reset(); _cHealthColor.Reset();
            _cHydrationAtMaximum.Reset(); _cHydrationCurrent.Reset(); _cHydrationMaximum.Reset();
            _cHydrationRate.Reset(); _cHydrationNormalized.Reset();
            _cHydrationWarningRate.Reset(); _cHydrationColor.Reset();
            _cEnergyAtMaximum.Reset(); _cEnergyCurrent.Reset(); _cEnergyMaximum.Reset();
            _cEnergyRate.Reset(); _cEnergyNormalized.Reset();
            _cEnergyWarningRate.Reset(); _cEnergyColor.Reset();
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
            var healthHUDModel = HealthHUDModel.Instance;
            var settingsModel = SettingsModel.Instance;

            if (!InputsChanged(healthHUDModel, settingsModel))
            {
                return;
            }

            //Set RectTransform anchoredPosition and sizeDelta and localScale
            _rectTransform.anchoredPosition = settingsModel.KeyAnchoredPosition.Value;
            _rectTransform.sizeDelta = settingsModel.KeySizeDelta.Value;
            _rectTransform.localScale = settingsModel.KeyLocalScale.Value;

            //Set Current and Maximum float

            #region overallHealth

            overallHealth.gameObject.SetActive(healthHUDModel.HealthHUDSw);
            overallHealth.buffHUDSw = settingsModel.KeyBuffSw.Value;
            overallHealth.arrowAnimation = settingsModel.KeyArrowAnimation.Value;
            overallHealth.arrowAnimationReverse = settingsModel.KeyArrowAnimationReverse.Value;
            overallHealth.isHealth = true;
            overallHealth.atMaximum = healthHUDModel.Health.Common.AtMaximum;

            overallHealth.current = healthHUDModel.Health.Common.Current;
            overallHealth.maximum = healthHUDModel.Health.Common.Maximum;
            overallHealth.buffRate = healthHUDModel.Health.HealthRate;
            overallHealth.normalized = healthHUDModel.Health.Common.Current / healthHUDModel.Health.Common.Maximum;
            overallHealth.warningRate = settingsModel.KeyHealthWarningRate.Value / 100f;
            overallHealth.buffSpeed = settingsModel.KeyBuffSpeed.Value;

            overallHealth.upBuffArrowColor = settingsModel.KeyUpBuffArrowColor.Value;
            overallHealth.downBuffArrowColor = settingsModel.KeyDownBuffArrowColor.Value;
            overallHealth.currentColor = settingsModel.KeyHealthColor.Value;
            overallHealth.maxColor = settingsModel.KeyMaxColor.Value;
            overallHealth.addZerosColor = settingsModel.KeyAddZerosColor.Value;
            overallHealth.warningColor = settingsModel.KeyWarningColor.Value;
            overallHealth.upBuffColor = settingsModel.KeyUpBuffColor.Value;
            overallHealth.downBuffColor = settingsModel.KeyDownBuffColor.Value;

            overallHealth.currentStyles = settingsModel.KeyCurrentStyles.Value;
            overallHealth.maximumStyles = settingsModel.KeyMaximumStyles.Value;

            #endregion

            #region hydration

            hydration.gameObject.SetActive(healthHUDModel.HealthHUDSw);
            hydration.buffHUDSw = settingsModel.KeyBuffSw.Value;
            hydration.arrowAnimation = settingsModel.KeyArrowAnimation.Value;
            hydration.arrowAnimationReverse = settingsModel.KeyArrowAnimationReverse.Value;
            hydration.isHealth = false;
            hydration.atMaximum = healthHUDModel.Health.Hydration.AtMaximum;

            hydration.current = healthHUDModel.Health.Hydration.Current;
            hydration.maximum = healthHUDModel.Health.Hydration.Maximum;
            hydration.buffRate = healthHUDModel.Health.HydrationRate;
            hydration.normalized = healthHUDModel.Health.Hydration.Normalized;
            hydration.warningRate = settingsModel.KeyHydrationWarningRate.Value / 100f;
            hydration.buffSpeed = settingsModel.KeyBuffSpeed.Value;

            hydration.upBuffArrowColor = settingsModel.KeyUpBuffArrowColor.Value;
            hydration.downBuffArrowColor = settingsModel.KeyDownBuffArrowColor.Value;
            hydration.currentColor = settingsModel.KeyHydrationColor.Value;
            hydration.maxColor = settingsModel.KeyMaxColor.Value;
            hydration.addZerosColor = settingsModel.KeyAddZerosColor.Value;
            hydration.warningColor = settingsModel.KeyWarningColor.Value;
            hydration.upBuffColor = settingsModel.KeyUpBuffColor.Value;
            hydration.downBuffColor = settingsModel.KeyDownBuffColor.Value;

            hydration.currentStyles = settingsModel.KeyCurrentStyles.Value;
            hydration.maximumStyles = settingsModel.KeyMaximumStyles.Value;

            #endregion

            #region energy

            energy.gameObject.SetActive(healthHUDModel.HealthHUDSw);
            energy.buffHUDSw = settingsModel.KeyBuffSw.Value;
            energy.arrowAnimation = settingsModel.KeyArrowAnimation.Value;
            energy.arrowAnimationReverse = settingsModel.KeyArrowAnimationReverse.Value;
            energy.isHealth = false;
            energy.atMaximum = healthHUDModel.Health.Energy.AtMaximum;

            energy.current = healthHUDModel.Health.Energy.Current;
            energy.maximum = healthHUDModel.Health.Energy.Maximum;
            energy.buffRate = healthHUDModel.Health.EnergyRate;
            energy.normalized = healthHUDModel.Health.Energy.Normalized;
            energy.warningRate = settingsModel.KeyEnergyWarningRate.Value / 100f;
            energy.buffSpeed = settingsModel.KeyBuffSpeed.Value;

            energy.upBuffArrowColor = settingsModel.KeyUpBuffArrowColor.Value;
            energy.downBuffArrowColor = settingsModel.KeyDownBuffArrowColor.Value;
            energy.currentColor = settingsModel.KeyEnergyColor.Value;
            energy.maxColor = settingsModel.KeyMaxColor.Value;
            energy.addZerosColor = settingsModel.KeyAddZerosColor.Value;
            energy.warningColor = settingsModel.KeyWarningColor.Value;
            energy.upBuffColor = settingsModel.KeyUpBuffColor.Value;
            energy.downBuffColor = settingsModel.KeyDownBuffColor.Value;

            energy.currentStyles = settingsModel.KeyCurrentStyles.Value;
            energy.maximumStyles = settingsModel.KeyMaximumStyles.Value;

            #endregion
        }

#endif
    }
}