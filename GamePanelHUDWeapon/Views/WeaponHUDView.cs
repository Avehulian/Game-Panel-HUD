using TMPro;
using UnityEngine;
#if !UNITY_EDITOR
using KmyTarkovUtils;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;
using GamePanelHUDWeapon.Models;
using SettingsModel = GamePanelHUDWeapon.Models.SettingsModel;

#endif

namespace GamePanelHUDWeapon.Views
{
    public class WeaponHUDView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        [SerializeField] private WeaponUIView weaponUIView;

        private RectTransform _rectTransform;

#if !UNITY_EDITOR

        private Changed<Vector2> _cAnchoredPosition, _cLocalScale;
        private Changed<bool> _cWeaponHUDSw, _cWeaponNameAlways, _cAmmoTypeHUDSw, _cFireModeHUDSw,
                              _cZeroWarning, _cWeaponShortNameSw;
        private Changed<int> _cMagCount, _cMagMaxCount, _cPatron, _cWarningRate10, _cWarningRate100;
        private Changed<float> _cNormalized, _cWeaponNameSpeed, _cZeroWarningSpeed;
        private Changed<string> _cWeaponShortName, _cWeaponName, _cAmmoType, _cFireMode;
        private Changed<Color> _cCurrentColor, _cMaxColor, _cPatronColor, _cWeaponNameColor,
                               _cAmmoTypeColor, _cFireModeColor, _cAddZerosColor, _cWarningColor;
        private Changed<FontStyles> _cCurrentStyles, _cMaximumStyles, _cPatronStyles,
                                    _cWeaponNameStyles, _cAmmoTypeStyles, _cFireModeStyles;

        /// <summary>
        /// True when any input differs from the previous frame. Uses non-short-circuiting | so EVERY
        /// TrySet runs and updates its snapshot -- with || the later comparisons would be skipped on the
        /// first change and their snapshots would go stale, silently freezing those values on screen.
        /// </summary>
        private bool InputsChanged(WeaponHUDModel weaponHUDModel, SettingsModel settingsModel)
        {
            return _cAnchoredPosition.TrySet(settingsModel.KeyAnchoredPosition.Value)
                 | _cLocalScale.TrySet(settingsModel.KeyLocalScale.Value)
                 | _cWeaponHUDSw.TrySet(weaponHUDModel.WeaponHUDSw)
                 | _cWeaponNameAlways.TrySet(weaponHUDModel.Weapon.WeaponNameAlways)
                 | _cAmmoTypeHUDSw.TrySet(settingsModel.KeyAmmoTypeHUDSw.Value)
                 | _cFireModeHUDSw.TrySet(settingsModel.KeyFireModeHUDSw.Value)
                 | _cZeroWarning.TrySet(settingsModel.KeyZeroWarning.Value)
                 | _cMagCount.TrySet(weaponHUDModel.Weapon.MagCount)
                 | _cMagMaxCount.TrySet(weaponHUDModel.Weapon.MagMaxCount)
                 | _cPatron.TrySet(weaponHUDModel.Weapon.Patron)
                 | _cNormalized.TrySet(weaponHUDModel.Weapon.Normalized)
                 | _cWeaponShortNameSw.TrySet(settingsModel.KeyWeaponShortName.Value)
                 | _cWeaponShortName.TrySet(weaponHUDModel.Weapon.WeaponShortName)
                 | _cWeaponName.TrySet(weaponHUDModel.Weapon.WeaponName)
                 | _cAmmoType.TrySet(weaponHUDModel.Weapon.AmmoType)
                 | _cFireMode.TrySet(weaponHUDModel.Weapon.FireMode)
                 | _cWarningRate10.TrySet(settingsModel.KeyWarningRate10.Value)
                 | _cWarningRate100.TrySet(settingsModel.KeyWarningRate100.Value)
                 | _cWeaponNameSpeed.TrySet(settingsModel.KeyWeaponNameSpeed.Value)
                 | _cZeroWarningSpeed.TrySet(settingsModel.KeyZeroWarningSpeed.Value)
                 | _cCurrentColor.TrySet(settingsModel.KeyCurrentColor.Value)
                 | _cMaxColor.TrySet(settingsModel.KeyMaxColor.Value)
                 | _cPatronColor.TrySet(settingsModel.KeyPatronColor.Value)
                 | _cWeaponNameColor.TrySet(settingsModel.KeyWeaponNameColor.Value)
                 | _cAmmoTypeColor.TrySet(settingsModel.KeyAmmoTypeColor.Value)
                 | _cFireModeColor.TrySet(settingsModel.KeyFireModeColor.Value)
                 | _cAddZerosColor.TrySet(settingsModel.KeyAddZerosColor.Value)
                 | _cWarningColor.TrySet(settingsModel.KeyWarningColor.Value)
                 | _cCurrentStyles.TrySet(settingsModel.KeyCurrentStyles.Value)
                 | _cMaximumStyles.TrySet(settingsModel.KeyMaximumStyles.Value)
                 | _cPatronStyles.TrySet(settingsModel.KeyPatronStyles.Value)
                 | _cWeaponNameStyles.TrySet(settingsModel.KeyWeaponNameStyles.Value)
                 | _cAmmoTypeStyles.TrySet(settingsModel.KeyAmmoTypeStyles.Value)
                 | _cFireModeStyles.TrySet(settingsModel.KeyFireModeStyles.Value);
        }

        private void ResetGate()
        {
            _cAnchoredPosition.Reset(); _cLocalScale.Reset();
            _cWeaponHUDSw.Reset(); _cWeaponNameAlways.Reset(); _cAmmoTypeHUDSw.Reset();
            _cFireModeHUDSw.Reset(); _cZeroWarning.Reset(); _cWeaponShortNameSw.Reset();
            _cMagCount.Reset(); _cMagMaxCount.Reset(); _cPatron.Reset();
            _cNormalized.Reset(); _cWeaponShortName.Reset(); _cWeaponName.Reset();
            _cAmmoType.Reset(); _cFireMode.Reset();
            _cWarningRate10.Reset(); _cWarningRate100.Reset();
            _cWeaponNameSpeed.Reset(); _cZeroWarningSpeed.Reset();
            _cCurrentColor.Reset(); _cMaxColor.Reset(); _cPatronColor.Reset(); _cWeaponNameColor.Reset();
            _cAmmoTypeColor.Reset(); _cFireModeColor.Reset(); _cAddZerosColor.Reset(); _cWarningColor.Reset();
            _cCurrentStyles.Reset(); _cMaximumStyles.Reset(); _cPatronStyles.Reset();
            _cWeaponNameStyles.Reset(); _cAmmoTypeStyles.Reset(); _cFireModeStyles.Reset();
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            WeaponHUDModel.Instance.WeaponTrigger = WeaponTrigger;
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
            var weaponHUDModel = WeaponHUDModel.Instance;
            var settingsModel = SettingsModel.Instance;

            if (!InputsChanged(weaponHUDModel, settingsModel))
            {
                return;
            }

            //Set RectTransform anchoredPosition and localScale
            _rectTransform.anchoredPosition = settingsModel.KeyAnchoredPosition.Value;
            _rectTransform.localScale = settingsModel.KeyLocalScale.Value;

            //Set Current Maximum Patron float
            weaponUIView.gameObject.SetActive(weaponHUDModel.WeaponHUDSw);
            weaponUIView.weaponNameAlways = weaponHUDModel.Weapon.WeaponNameAlways;
            weaponUIView.ammoTypeHUDSw = settingsModel.KeyAmmoTypeHUDSw.Value;
            weaponUIView.fireModeHUDSw = settingsModel.KeyFireModeHUDSw.Value;
            weaponUIView.zeroWarning = settingsModel.KeyZeroWarning.Value;

            weaponUIView.current = weaponHUDModel.Weapon.MagCount;
            weaponUIView.maximum = weaponHUDModel.Weapon.MagMaxCount;
            weaponUIView.patron = weaponHUDModel.Weapon.Patron;
            weaponUIView.normalized = weaponHUDModel.Weapon.Normalized;
            weaponUIView.weaponName = settingsModel.KeyWeaponShortName.Value
                ? weaponHUDModel.Weapon.WeaponShortName
                : weaponHUDModel.Weapon.WeaponName;
            weaponUIView.ammoType = weaponHUDModel.Weapon.AmmoType;
            weaponUIView.fireMode = weaponHUDModel.Weapon.FireMode;

            weaponUIView.warningRate10 = settingsModel.KeyWarningRate10.Value / 100f;
            weaponUIView.warningRate100 = settingsModel.KeyWarningRate100.Value / 100f;
            weaponUIView.weaponNameSpeed = settingsModel.KeyWeaponNameSpeed.Value;
            weaponUIView.zeroWarningSpeed = settingsModel.KeyZeroWarningSpeed.Value;

            weaponUIView.currentColor = settingsModel.KeyCurrentColor.Value;
            weaponUIView.maxColor = settingsModel.KeyMaxColor.Value;
            weaponUIView.patronColor = settingsModel.KeyPatronColor.Value;
            weaponUIView.weaponNameColor = settingsModel.KeyWeaponNameColor.Value;
            weaponUIView.ammoTypeColor = settingsModel.KeyAmmoTypeColor.Value;
            weaponUIView.fireModeColor = settingsModel.KeyFireModeColor.Value;
            weaponUIView.addZerosColor = settingsModel.KeyAddZerosColor.Value;
            weaponUIView.warningColor = settingsModel.KeyWarningColor.Value;

            weaponUIView.currentStyles = settingsModel.KeyCurrentStyles.Value;
            weaponUIView.maximumStyles = settingsModel.KeyMaximumStyles.Value;
            weaponUIView.patronStyles = settingsModel.KeyPatronStyles.Value;
            weaponUIView.weaponNameStyles = settingsModel.KeyWeaponNameStyles.Value;
            weaponUIView.ammoTypeStyles = settingsModel.KeyAmmoTypeStyles.Value;
            weaponUIView.fireModeStyles = settingsModel.KeyFireModeStyles.Value;
        }

        private void WeaponTrigger()
        {
            weaponUIView.weaponIsTrigger = true;
        }

#endif
    }
}