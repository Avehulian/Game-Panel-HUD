using System;
using TMPro;
using UnityEngine;
#if !UNITY_EDITOR
using KmyTarkovUtils;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;
using GamePanelHUDHit.Models;
using SettingsModel = GamePanelHUDHit.Models.SettingsModel;

#endif

namespace GamePanelHUDHit.Views
{
    public class HitHUDView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        [SerializeField] private HitUIView hitUIView;

        private HitUIView _testHitUIView;

        private CanvasGroup _hitGroup;

#if !UNITY_EDITOR

        private Changed<bool> _cHitHUDSw, _cDamageHUDSw;
        private Changed<Vector2> _cHitAnchoredPosition, _cHitSizeDelta, _cHitLocalScale, _cHitHeadSizeDelta,
                                 _cHitDamageAnchoredPosition, _cHitDamageSizeDelta, _cHitDamageLocalScale;
        private Changed<Vector3> _cHitLocalRotation;
        private Changed<float> _cActiveSpeed, _cEndSpeed, _cDeadSpeed;
        private Changed<Color> _cDamageColor, _cArmorDamageColor, _cDeadColor, _cHeadColor,
                               _cDamageInfoColor, _cArmorDamageInfoColor;
        private Changed<FontStyles> _cDamageStyles, _cArmorDamageStyles;

        /// <summary>
        /// True when any input differs from the previous frame. Uses non-short-circuiting | so EVERY
        /// TrySet runs and updates its snapshot -- with || the later comparisons would be skipped on the
        /// first change and their snapshots would go stale, silently freezing those values on screen.
        /// </summary>
        private bool InputsChanged(SettingsModel settingsModel)
        {
            return _cHitHUDSw.TrySet(HitHUDModel.Instance.HitHUDSw)
                 | _cDamageHUDSw.TrySet(settingsModel.KeyHitDamageHUDSw.Value)
                 | _cHitAnchoredPosition.TrySet(settingsModel.KeyHitAnchoredPosition.Value)
                 | _cHitLocalRotation.TrySet(settingsModel.KeyHitLocalRotation.Value)
                 | _cHitSizeDelta.TrySet(settingsModel.KeyHitSizeDelta.Value)
                 | _cHitLocalScale.TrySet(settingsModel.KeyHitLocalScale.Value)
                 | _cHitHeadSizeDelta.TrySet(settingsModel.KeyHitHeadSizeDelta.Value)
                 | _cHitDamageAnchoredPosition.TrySet(settingsModel.KeyHitDamageAnchoredPosition.Value)
                 | _cHitDamageSizeDelta.TrySet(settingsModel.KeyHitDamageSizeDelta.Value)
                 | _cHitDamageLocalScale.TrySet(settingsModel.KeyHitDamageLocalScale.Value)
                 | _cActiveSpeed.TrySet(settingsModel.KeyHitActiveSpeed.Value)
                 | _cEndSpeed.TrySet(settingsModel.KeyHitEndSpeed.Value)
                 | _cDeadSpeed.TrySet(settingsModel.KeyHitDeadSpeed.Value)
                 | _cDamageColor.TrySet(settingsModel.KeyHitDamageColor.Value)
                 | _cArmorDamageColor.TrySet(settingsModel.KeyHitArmorDamageColor.Value)
                 | _cDeadColor.TrySet(settingsModel.KeyHitDeadColor.Value)
                 | _cHeadColor.TrySet(settingsModel.KeyHitHeadColor.Value)
                 | _cDamageInfoColor.TrySet(settingsModel.KeyHitDamageInfoColor.Value)
                 | _cArmorDamageInfoColor.TrySet(settingsModel.KeyHitArmorDamageInfoColor.Value)
                 | _cDamageStyles.TrySet(settingsModel.KeyHitDamageStyles.Value)
                 | _cArmorDamageStyles.TrySet(settingsModel.KeyHitArmorDamageStyles.Value);
        }

        private void ResetGate()
        {
            _cHitHUDSw.Reset(); _cDamageHUDSw.Reset();
            _cHitAnchoredPosition.Reset(); _cHitLocalRotation.Reset(); _cHitSizeDelta.Reset();
            _cHitLocalScale.Reset(); _cHitHeadSizeDelta.Reset();
            _cHitDamageAnchoredPosition.Reset(); _cHitDamageSizeDelta.Reset(); _cHitDamageLocalScale.Reset();
            _cActiveSpeed.Reset(); _cEndSpeed.Reset(); _cDeadSpeed.Reset();
            _cDamageColor.Reset(); _cArmorDamageColor.Reset(); _cDeadColor.Reset(); _cHeadColor.Reset();
            _cDamageInfoColor.Reset(); _cArmorDamageInfoColor.Reset();
            _cDamageStyles.Reset(); _cArmorDamageStyles.Reset();
        }

        private void Awake()
        {
            _testHitUIView = Instantiate(hitUIView, transform);

            _hitGroup = hitUIView.GetComponent<CanvasGroup>();

            HitHUDModel.Instance.ShowHit = ShowHit;
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

            if (!InputsChanged(settingsModel))
            {
                return;
            }

            _hitGroup.alpha = HitHUDModel.Instance.HitHUDSw ? 1 : 0;

            HitSet(hitUIView);
            HitSet(_testHitUIView);
        }

        private static void HitSet(HitUIView hitUIView)
        {
            var settingsModel = SettingsModel.Instance;

            hitUIView.damageHUDSw = settingsModel.KeyHitDamageHUDSw.Value;

            hitUIView.hitAnchoredPosition = settingsModel.KeyHitAnchoredPosition.Value;
            hitUIView.hitLocalRotation = settingsModel.KeyHitLocalRotation.Value;
            hitUIView.hitSizeDelta = settingsModel.KeyHitSizeDelta.Value;
            hitUIView.hitLocalScale = settingsModel.KeyHitLocalScale.Value;
            hitUIView.hitHeadSizeDelta = settingsModel.KeyHitHeadSizeDelta.Value;

            hitUIView.hitDamageAnchoredPosition = settingsModel.KeyHitDamageAnchoredPosition.Value;
            hitUIView.hitDamageSizeDelta = settingsModel.KeyHitDamageSizeDelta.Value;
            hitUIView.hitDamageLocalScale = settingsModel.KeyHitDamageLocalScale.Value;

            hitUIView.activeSpeed = settingsModel.KeyHitActiveSpeed.Value;
            hitUIView.endSpeed = settingsModel.KeyHitEndSpeed.Value;
            hitUIView.deadSpeed = settingsModel.KeyHitDeadSpeed.Value;

            hitUIView.damageColor = settingsModel.KeyHitDamageColor.Value;
            hitUIView.armorDamageColor = settingsModel.KeyHitArmorDamageColor.Value;
            hitUIView.deadColor = settingsModel.KeyHitDeadColor.Value;
            hitUIView.headColor = settingsModel.KeyHitHeadColor.Value;
            hitUIView.damageInfoColor = settingsModel.KeyHitDamageInfoColor.Value;
            hitUIView.armorDamageInfoColor = settingsModel.KeyHitArmorDamageInfoColor.Value;

            hitUIView.damageStyles = settingsModel.KeyHitDamageStyles.Value;
            hitUIView.armorDamageStyles = settingsModel.KeyHitArmorDamageStyles.Value;
        }

        private void ShowHit(HitModel hitModel)
        {
            var hitHUDModel = HitHUDModel.Instance;
            var settingsModel = SettingsModel.Instance;

            HitModel.Direction direction;
            if (hitModel.HitDirection.x < settingsModel.KeyHitDirectionLeft.Value)
            {
                direction = HitModel.Direction.Left;
            }
            else if (hitModel.HitDirection.x > settingsModel.KeyHitDirectionRight.Value)
            {
                direction = HitModel.Direction.Right;
            }
            else
            {
                direction = HitModel.Direction.Center;
            }

            if (!hitModel.IsTest)
            {
                var cam = Camera.main;

                if (cam != null)
                {
                    var pos = cam.WorldToScreenPoint(hitModel.HitPoint);

                    var screenPos = new Vector2(pos.x - cam.pixelWidth * 0.5f, pos.y - cam.pixelHeight * 0.5f);

                    hitUIView.transform.localPosition =
                        new Vector2((int)Math.Round(screenPos.x), (int)Math.Round(screenPos.y));
                }

                Hit(hitModel, direction, settingsModel, hitUIView);
            }
            else
            {
                _testHitUIView.transform.localPosition = new Vector2(hitHUDModel.ScreenRect.sizeDelta.x / 3.2f, 0);

                Hit(hitModel, direction, settingsModel, _testHitUIView);
            }
        }

        private static void Hit(HitModel hitModel,
            HitModel.Direction direction, SettingsModel settingsModel,
            HitUIView hitUIView)
        {
            hitUIView.damage = hitModel.Damage;
            hitUIView.armorDamage = hitModel.ArmorDamage;

            var isHead = hitModel.DamagePart == EBodyPart.Head;

            if (settingsModel.KeyHitHasHead.Value && isHead && hitModel.HitType != HitModel.Hit.Dead)
            {
                hitModel.HitType = HitModel.Hit.Head;
            }

            hitUIView.hasArmorHit = hitModel.HasArmorHit;

            hitUIView.CustomUpdate();

            if (hitModel.HitType == HitModel.Hit.Dead)
            {
                hitUIView.HitDeadTrigger();
            }

            if (!settingsModel.KeyHitHasDirection.Value)
            {
                direction = HitModel.Direction.Center;
            }

            hitUIView.HitTrigger(isHead, hitModel, direction);
        }

#endif
    }
}