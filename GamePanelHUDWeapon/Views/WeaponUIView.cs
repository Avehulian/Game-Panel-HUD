using TMPro;
using UnityEngine;
#if !UNITY_EDITOR
using KmyTarkovUtils;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;

#endif

namespace GamePanelHUDWeapon.Views
{
    public class WeaponUIView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        public bool weaponNameAlways;

        public bool ammoTypeHUDSw;

        public bool fireModeHUDSw;

        public bool zeroWarning;

        public bool weaponIsTrigger;

        public int current;

        public int maximum;

        public int patron;

        public float normalized;

        public string weaponName;

        public string ammoType;

        public string fireMode;

        public float warningRate10;

        public float warningRate100;

        public float weaponNameSpeed;

        public float zeroWarningSpeed;

        public Color currentColor;

        public Color maxColor;

        public Color patronColor;

        public Color weaponNameColor;

        public Color ammoTypeColor;

        public Color fireModeColor;

        public Color addZerosColor;

        public Color warningColor;

        public FontStyles currentStyles;

        public FontStyles maximumStyles;

        public FontStyles patronStyles;

        public FontStyles weaponNameStyles;

        public FontStyles ammoTypeStyles;

        public FontStyles fireModeStyles;

        [SerializeField] private TMP_Text zerosValue;

        [SerializeField] private TMP_Text currentValue;

        [SerializeField] private TMP_Text maxSignValue;

        [SerializeField] private TMP_Text maxValue;

        [SerializeField] private TMP_Text patronSignValue;

        [SerializeField] private TMP_Text patronValue;

        [SerializeField] private TMP_Text weaponNameValue;

        [SerializeField] private TMP_Text ammoTypeValue;

        [SerializeField] private TMP_Text fireModeValue;

        private Animator _animatorWeaponName;

        private Animator _animatorCurrent;

        private Transform _patronPanelTransform;

        private Transform _fireModePanelTransform;

#if !UNITY_EDITOR

        private Changed<int> _cCurrent, _cMaximum, _cPatron;
        private Changed<float> _cNormalized, _cWarningRate10, _cWarningRate100, _cWeaponNameSpeed, _cZeroWarningSpeed;
        private Changed<bool> _cWeaponNameAlways, _cAmmoTypeHUDSw, _cFireModeHUDSw, _cZeroWarning, _cWeaponIsTrigger;
        private Changed<string> _cWeaponName, _cAmmoType, _cFireMode;
        private Changed<Color> _cCurrentColor, _cMaxColor, _cPatronColor, _cWeaponNameColor,
                               _cAmmoTypeColor, _cFireModeColor, _cAddZerosColor, _cWarningColor;
        private Changed<FontStyles> _cCurrentStyles, _cMaximumStyles, _cPatronStyles,
                                    _cWeaponNameStyles, _cAmmoTypeStyles, _cFireModeStyles;

        /// <summary>
        /// True when any input differs from the previous frame. Uses non-short-circuiting | so EVERY
        /// TrySet runs and updates its snapshot -- with || the later comparisons would be skipped on the
        /// first change and their snapshots would go stale, silently freezing those values on screen.
        /// </summary>
        private bool InputsChanged()
        {
            return _cCurrent.TrySet(current)
                 | _cMaximum.TrySet(maximum)
                 | _cPatron.TrySet(patron)
                 | _cNormalized.TrySet(normalized)
                 | _cWeaponNameAlways.TrySet(weaponNameAlways)
                 | _cAmmoTypeHUDSw.TrySet(ammoTypeHUDSw)
                 | _cFireModeHUDSw.TrySet(fireModeHUDSw)
                 | _cZeroWarning.TrySet(zeroWarning)
                 | _cWeaponIsTrigger.TrySet(weaponIsTrigger)
                 | _cWeaponName.TrySet(weaponName)
                 | _cAmmoType.TrySet(ammoType)
                 | _cFireMode.TrySet(fireMode)
                 | _cWarningRate10.TrySet(warningRate10)
                 | _cWarningRate100.TrySet(warningRate100)
                 | _cWeaponNameSpeed.TrySet(weaponNameSpeed)
                 | _cZeroWarningSpeed.TrySet(zeroWarningSpeed)
                 | _cCurrentColor.TrySet(currentColor)
                 | _cMaxColor.TrySet(maxColor)
                 | _cPatronColor.TrySet(patronColor)
                 | _cWeaponNameColor.TrySet(weaponNameColor)
                 | _cAmmoTypeColor.TrySet(ammoTypeColor)
                 | _cFireModeColor.TrySet(fireModeColor)
                 | _cAddZerosColor.TrySet(addZerosColor)
                 | _cWarningColor.TrySet(warningColor)
                 | _cCurrentStyles.TrySet(currentStyles)
                 | _cMaximumStyles.TrySet(maximumStyles)
                 | _cPatronStyles.TrySet(patronStyles)
                 | _cWeaponNameStyles.TrySet(weaponNameStyles)
                 | _cAmmoTypeStyles.TrySet(ammoTypeStyles)
                 | _cFireModeStyles.TrySet(fireModeStyles);
        }

        private void ResetGate()
        {
            _cCurrent.Reset(); _cMaximum.Reset(); _cPatron.Reset(); _cNormalized.Reset();
            _cWeaponNameAlways.Reset(); _cAmmoTypeHUDSw.Reset(); _cFireModeHUDSw.Reset();
            _cZeroWarning.Reset(); _cWeaponIsTrigger.Reset();
            _cWeaponName.Reset(); _cAmmoType.Reset(); _cFireMode.Reset();
            _cWarningRate10.Reset(); _cWarningRate100.Reset();
            _cWeaponNameSpeed.Reset(); _cZeroWarningSpeed.Reset();
            _cCurrentColor.Reset(); _cMaxColor.Reset(); _cPatronColor.Reset(); _cWeaponNameColor.Reset();
            _cAmmoTypeColor.Reset(); _cFireModeColor.Reset(); _cAddZerosColor.Reset(); _cWarningColor.Reset();
            _cCurrentStyles.Reset(); _cMaximumStyles.Reset(); _cPatronStyles.Reset();
            _cWeaponNameStyles.Reset(); _cAmmoTypeStyles.Reset(); _cFireModeStyles.Reset();
        }

        private void Awake()
        {
            _animatorWeaponName = weaponNameValue.transform.parent.GetComponent<Animator>();
            _animatorCurrent = currentValue.GetComponent<Animator>();

            _patronPanelTransform = patronValue.transform.parent;
            _fireModePanelTransform = fireModeValue.transform.parent;
        }

        private void Start()
        {
            HUDCoreModel.Instance.UpdateManger.Register(this);
        }

        private void OnEnable()
        {
            // ResetGate before Run: if Run dispatches CustomUpdate synchronously, the first repaint
            // would otherwise evaluate stale snapshots and slip a frame.
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

            //Set Current float and color and Style to String
            Color currentValueColor;
            if (current != 0 && (normalized > warningRate10 && maximum <= 10 ||
                                 normalized >= warningRate100 && maximum > 10))
            {
                currentValueColor = currentColor;
            }
            else
            {
                currentValueColor = warningColor;
            }

            if (current < 10 && maximum > 10)
            {
                zerosValue.gameObject.SetActive(true);
                currentValue.alignment = TextAlignmentOptions.TopLeft;
            }
            else
            {
                zerosValue.gameObject.SetActive(false);
                currentValue.alignment = TextAlignmentOptions.TopRight;
            }

            zerosValue.fontStyle = currentStyles;
            zerosValue.color = addZerosColor;

            currentValue.fontStyle = currentStyles;
            currentValue.color = currentValueColor;
            currentValue.text = current.ToString();

            maxSignValue.fontStyle = maximumStyles;
            maxSignValue.color = maxColor;

            //Set Maximum float and color and Style to String
            maxValue.fontStyle = maximumStyles;
            maxValue.color = maxColor;
            maxValue.text = maximum.ToString();

            patronSignValue.fontStyle = patronStyles;
            patronSignValue.color = patronColor;

            //Patron HUD display
            _patronPanelTransform.gameObject.SetActive(patron != 0);

            //Set Patron float and color and Style to String
            patronValue.fontStyle = patronStyles;
            patronValue.color = patronColor;
            patronValue.text = patron.ToString();

            //Set Weapon Name
            weaponNameValue.fontStyle = weaponNameStyles;
            weaponNameValue.color = weaponNameColor;
            weaponNameValue.text = weaponName;

            _animatorWeaponName.SetBool(AnimatorHash.Always, weaponNameAlways);
            _animatorWeaponName.SetFloat(AnimatorHash.Speed, weaponNameSpeed);

            _animatorCurrent.SetBool(AnimatorHash.Zero, current == 0 && patron == 0 && zeroWarning);
            _animatorCurrent.SetFloat(AnimatorHash.Speed, zeroWarningSpeed);

            //Fire Mode HUD display
            _fireModePanelTransform.gameObject.SetActive(fireModeHUDSw);

            //Set Fire Mode
            fireModeValue.fontStyle = fireModeStyles;
            fireModeValue.color = fireModeColor;
            fireModeValue.text = fireMode;

            ammoTypeValue.gameObject.SetActive(ammoTypeHUDSw);

            ammoTypeValue.fontStyle = ammoTypeStyles;
            ammoTypeValue.color = ammoTypeColor;
            ammoTypeValue.text = ammoType;

            if (!weaponIsTrigger)
                return;

            _animatorWeaponName.SetTrigger(AnimatorHash.Active);

            weaponIsTrigger = false;
        }

#endif
    }
}