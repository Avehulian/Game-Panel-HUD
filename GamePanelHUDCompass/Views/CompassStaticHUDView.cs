using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_EDITOR
using EFT;
using KmyTarkovUtils;
using static KmyTarkovApi.EFTHelpers;
using GamePanelHUDCore.Models;
using GamePanelHUDCore.Utils;
using GamePanelHUDCompass.Models;
using GamePanelHUDCompass.VisitApi;
using SettingsModel = GamePanelHUDCompass.Models.SettingsModel;

#endif

namespace GamePanelHUDCompass.Views
{
    public class CompassStaticHUDView : MonoBehaviour
#if !UNITY_EDITOR
        , KmyTarkovUtils.IUpdate
#endif
    {
        private static readonly ConcurrentDictionary<string, List<CompassStaticUIView>> CompassStatics =
            new ConcurrentDictionary<string, List<CompassStaticUIView>>();

        private static readonly List<string> Removes = new List<string>();

        [SerializeField] private Transform compassStaticRoot;

        [SerializeField] private Transform airdropsRoot;

        [SerializeField] private Transform questsRoot;

        [SerializeField] private Transform exfiltrationsRoot;

        [SerializeField] private Sprite airdrop;

        [SerializeField] private Sprite exfiltration;

        [SerializeField] private TMP_Text nameValue;

        [SerializeField] private TMP_Text necessaryValue;

        [SerializeField] private TMP_Text requirementsValue;

        [SerializeField] private TMP_Text descriptionValue;

        [SerializeField] private TMP_Text distanceValue;

        [SerializeField] private TMP_Text distanceSignValue;

        [SerializeField] private Image inZone;

        private Transform _infoPanelTransform;

        private Transform _distancePanelTransform;

        private RectTransform _rectTransform;

        private RectTransform _namePanelRect;

        private RectTransform _descriptionPanelRect;

        private RectTransform _infoPanelRect;

        private CanvasGroup _compassStaticGroup;

#if !UNITY_EDITOR

        // Perf merge (upstream f67fce0): these RectTransform/CanvasGroup/gameObject writes ran every
        // frame and every write dirties the canvas - the gates only let changed values through.
        private Changed<float> _cGroupAlpha;

        private Changed<Vector2> _cAnchoredPosition, _cSizeDelta, _cLocalScale, _cOutDistanceHalfSize,
            _cInfoAnchoredPosition, _cInfoScale;

        private Changed<bool> _cStaticHUDSw, _cAirdrop, _cExfiltration, _cQuest;

        /// <summary>Story objective markers whose chapter icon is still being downloaded by VisitAPI.</summary>
        private sealed class PendingChapterIcon
        {
            public CompassStaticUIView View;

            public string Url;

            public int Frames;
        }

        private static readonly List<PendingChapterIcon> PendingChapterIcons =
            new List<PendingChapterIcon>();

        private static int _chapterIconCooldown;

        private const string StoryObjectiveText = "Story objective";

        private void ResetGate()
        {
            _cGroupAlpha.Reset();
            _cAnchoredPosition.Reset(); _cSizeDelta.Reset(); _cLocalScale.Reset(); _cOutDistanceHalfSize.Reset();
            _cStaticHUDSw.Reset(); _cAirdrop.Reset(); _cExfiltration.Reset(); _cQuest.Reset();
            _cInfoAnchoredPosition.Reset(); _cInfoScale.Reset();
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            _namePanelRect = nameValue.transform.parent.GetComponent<RectTransform>();
            _descriptionPanelRect = descriptionValue.GetComponent<RectTransform>();

            _distancePanelTransform = distanceValue.transform.parent;

            _infoPanelTransform = _namePanelRect.parent;
            _infoPanelRect = _infoPanelTransform.GetComponent<RectTransform>();

            _compassStaticGroup = compassStaticRoot.GetComponent<CanvasGroup>();

            // Perf merge (upstream a9abe6a): a nested Canvas isolates rebuilds - dirtying a child
            // canvas does not dirty its parent. The compass markers move every frame, so without
            // this they force a rebuild of the entire shared HUD canvas, whose cost scales with
            // everything on it rather than just what moved.
            var compassCanvas = compassStaticRoot.gameObject.GetComponent<Canvas>();

            if (compassCanvas == null)
            {
                compassStaticRoot.gameObject.AddComponent<Canvas>();
            }

            // (No overrideSorting here: the upstream commit only adds the Canvas - overriding the
            // sorting draws this panel over the compass strip.)

            var compassStaticHUDModel = CompassStaticHUDModel.Instance;

            compassStaticHUDModel.ShowStatic = ShowStatic;
            compassStaticHUDModel.DestroyStatic = DestroyStaticUI;
        }

        private void Start()
        {
            HUDCoreModel.Instance.WorldDispose += DestroyAll;

            HUDCoreModel.Instance.UpdateManger.Register(this);
        }

        // Drop the snapshots whenever this view comes back, so the first frame after re-enable
        // repaints unconditionally instead of trusting state torn down while we were off.
        private void OnEnable()
        {
            ResetGate();
        }

        public void CustomUpdate()
        {
            var compassHUDModel = CompassHUDModel.Instance;
            var compassFireHUDModel = CompassFireHUDModel.Instance;
            var compassStaticHUDModel = CompassStaticHUDModel.Instance;
            var settingsModel = SettingsModel.Instance;

            ResolvePendingChapterIcons();

            var targetAlpha = settingsModel.KeyImmersiveCompass.Value
                ? compassHUDModel.Compass.CompassState ? 1f : 0f
                : 1f;

            if (_cGroupAlpha.TrySet(targetAlpha))
                _compassStaticGroup.alpha = targetAlpha;

            var anchoredPosition = settingsModel.KeyAnchoredPosition.Value;

            if (_cAnchoredPosition.TrySet(anchoredPosition))
                _rectTransform.anchoredPosition = anchoredPosition;

            var sizeDelta = new Vector2(compassHUDModel.Compass.SizeDelta.x,
                compassHUDModel.Compass.SizeDelta.y * 1.5f);

            if (_cSizeDelta.TrySet(sizeDelta))
                _rectTransform.sizeDelta = sizeDelta;

            var localScale = settingsModel.KeyLocalScale.Value;

            if (_cLocalScale.TrySet(localScale))
                _rectTransform.localScale = localScale;

            //Distance panel width is 110, Spacing is 5
            var outDistanceHalfSize = settingsModel.KeyCompassStaticInfoAutoSizeDelta.Value
                ? new Vector2((compassHUDModel.Compass.SizeDelta.x - 110 - 5 - 10) / 2,
                    settingsModel.KeyCompassStaticInfoSizeDelta.Value.y)
                : settingsModel.KeyCompassStaticInfoSizeDelta.Value;

            if (_cOutDistanceHalfSize.TrySet(outDistanceHalfSize))
            {
                _namePanelRect.sizeDelta = outDistanceHalfSize;
                _descriptionPanelRect.sizeDelta = outDistanceHalfSize;
            }

            var staticHUDSw = compassStaticHUDModel.CompassStaticHUDSw;

            if (_cStaticHUDSw.TrySet(staticHUDSw))
                compassStaticRoot.gameObject.SetActive(staticHUDSw);

            var airdropSw = settingsModel.KeyCompassStaticAirdrop.Value;

            if (_cAirdrop.TrySet(airdropSw))
                airdropsRoot.gameObject.SetActive(airdropSw);

            var exfiltrationSw = settingsModel.KeyCompassStaticExfiltration.Value;

            if (_cExfiltration.TrySet(exfiltrationSw))
                exfiltrationsRoot.gameObject.SetActive(exfiltrationSw);

            var questSw = settingsModel.KeyCompassStaticQuest.Value;

            if (_cQuest.TrySet(questSw))
                questsRoot.gameObject.SetActive(questSw);

            var infoAnchoredPosition = settingsModel.KeyCompassStaticInfoAnchoredPosition.Value;

            if (_cInfoAnchoredPosition.TrySet(infoAnchoredPosition))
                _infoPanelRect.anchoredPosition = infoAnchoredPosition;

            var infoScale = settingsModel.KeyCompassStaticInfoScale.Value;

            if (_cInfoScale.TrySet(infoScale))
                _infoPanelRect.localScale = infoScale;

            var isCenter = false;
            if (CompassStatics.Count > 0)
            {
                if (Removes.Count > 0)
                {
                    // Drain from the end: removing index i inside a forward loop skipped the next
                    // entry, and ids whose marker was already gone stayed queued forever - a later
                    // ShowStatic for the same id was then destroyed by that stale entry.
                    for (var i = Removes.Count - 1; i >= 0; i--)
                    {
                        var remove = Removes[i];

                        Removes.RemoveAt(i);

                        if (!CompassStatics.TryRemove(remove, out var list))
                            continue;

                        foreach (var ui in list)
                        {
                            ui.Destroy();
                        }
                    }
                }

                var range = settingsModel.KeyCompassStaticCenterPointRange.Value;

                List<(float XDiff, CompassStaticUIView StaticUI)> allDiff =
                    new List<(float, CompassStaticUIView)>();
                foreach (var uis in CompassStatics.Values)
                {
                    foreach (var ui in uis)
                    {
                        if (!ui.Work)
                            continue;

                        var xDiff = ui.XDiff;
                        var xLeftDiff = ui.XLeftDiff;
                        var xRightDiff = ui.XRightDiff;

                        if (xDiff < range && xDiff > -range)
                        {
                            allDiff.Add((xDiff, ui));
                            isCenter = true;
                        }
                        else if (xLeftDiff < range && xLeftDiff > -range)
                        {
                            allDiff.Add((xLeftDiff, ui));
                            isCenter = true;
                        }
                        else if (xRightDiff < range && xRightDiff > -range)
                        {
                            allDiff.Add((xRightDiff, ui));
                            isCenter = true;
                        }
                    }
                }

                if (isCenter)
                {
                    float minXDiff = range;
                    CompassStaticUIView minUI = null;

                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var diff in allDiff)
                    {
                        if (Math.Abs(diff.XDiff) > minXDiff)
                            continue;

                        minXDiff = diff.XDiff;

                        minUI = diff.StaticUI;
                    }

                    if (minUI != null)
                    {
                        switch (minUI.InfoType)
                        {
                            case StaticModel.Type.Airdrop:
                                airdropsRoot.SetAsLastSibling();
                                break;
                            case StaticModel.Type.Exfiltration:
                            case StaticModel.Type.Switch:
                            case StaticModel.Type.Transit:
                                exfiltrationsRoot.SetAsLastSibling();
                                break;
                            case StaticModel.Type.ConditionLeaveItemAtLocation:
                            case StaticModel.Type.ConditionPlaceBeacon:
                            case StaticModel.Type.ConditionFindItem:
                            case StaticModel.Type.ConditionVisitPlace:
                            case StaticModel.Type.ConditionInZone:
                            case StaticModel.Type.VisitZone:
                                questsRoot.SetAsLastSibling();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(minUI.InfoType), minUI.InfoType, null);
                        }

                        minUI.transform.SetAsLastSibling();
                        var necessaryText = minUI.isNotNecessary
                            ? _LocalizedHelper.Localized("(optional)")
                            : string.Empty;

                        var textInfo = CultureInfo.CurrentCulture.TextInfo;
                        var requirementsText = minUI.HasRequirements
                            ? $"({textInfo.ToTitleCase(_LocalizedHelper.Localized("ragfair/REQUIREMENTS", EStringCase.Lower))})"
                            : string.Empty;

                        var nameStyles = settingsModel.KeyCompassStaticNameStyles.Value;

                        nameValue.fontStyle = nameStyles;
                        nameValue.color = settingsModel.KeyCompassStaticNameColor.Value;
                        nameValue.text = string.IsNullOrEmpty(minUI.nameText)
                            ? ResolveName(minUI)
                            : minUI.nameText;

                        necessaryValue.gameObject.SetActive(minUI.isNotNecessary);
                        requirementsValue.gameObject.SetActive(minUI.HasRequirements);

                        necessaryValue.fontStyle = nameStyles;
                        necessaryValue.text = necessaryText;
                        necessaryValue.color = settingsModel.KeyCompassStaticNecessaryColor.Value;

                        requirementsValue.fontStyle = nameStyles;
                        requirementsValue.color = settingsModel.KeyCompassStaticRequirementsColor.Value;
                        requirementsValue.text = requirementsText;

                        descriptionValue.fontStyle = settingsModel.KeyCompassStaticDescriptionStyles.Value;
                        descriptionValue.color = settingsModel.KeyCompassStaticDescriptionColor.Value;
                        descriptionValue.text = string.IsNullOrEmpty(minUI.descriptionText)
                            ? ResolveDescription(minUI)
                            : minUI.descriptionText;

                        var distanceStyles = settingsModel.KeyCompassStaticDistanceStyles.Value;

                        var distance = Vector3.Distance(minUI.where, compassFireHUDModel.CompassFire.CameraPosition)
                            .ToString("F0");

                        distanceValue.fontStyle = distanceStyles;
                        distanceValue.color = settingsModel.KeyCompassStaticDistanceColor.Value;
                        distanceValue.text = distance;

                        distanceSignValue.fontStyle = distanceStyles;
                        distanceSignValue.color = settingsModel.KeyCompassStaticMetersColor.Value;

                        inZone.gameObject.SetActive(settingsModel.KeyCompassStaticInZoneHUDSw.Value && minUI.InZone);
                        inZone.color = settingsModel.KeyCompassStaticInZoneColor.Value;
                    }
                }
            }

            if (!isCenter)
            {
                questsRoot.SetSiblingIndex(0);
                exfiltrationsRoot.SetSiblingIndex(1);
                airdropsRoot.SetSiblingIndex(2);
            }

            _infoPanelTransform.gameObject.SetActive(isCenter && settingsModel.KeyCompassStaticInfoHUDSw.Value);

            _distancePanelTransform.gameObject.SetActive(settingsModel.KeyCompassStaticDistanceHUDSw.Value);
        }

        private void ShowStatic(StaticModel staticModel)
        {
            CompassStatics.AddOrUpdate(staticModel.Id,
                key => new List<CompassStaticUIView> { CreateStatic(staticModel) },
                (key, value) =>
                {
                    if (value == null)
                        return null;

                    value.Add(CreateStatic(staticModel));

                    return value;
                });
        }

        private CompassStaticUIView CreateStatic(StaticModel staticModel)
        {
            Transform root;
            switch (staticModel.InfoType)
            {
                case StaticModel.Type.Airdrop:
                    root = airdropsRoot;
                    break;
                case StaticModel.Type.Exfiltration:
                case StaticModel.Type.Switch:
                case StaticModel.Type.Transit:
                    root = exfiltrationsRoot;
                    break;
                case StaticModel.Type.ConditionLeaveItemAtLocation:
                case StaticModel.Type.ConditionPlaceBeacon:
                case StaticModel.Type.ConditionFindItem:
                case StaticModel.Type.ConditionVisitPlace:
                case StaticModel.Type.ConditionInZone:
                case StaticModel.Type.VisitZone:
                    root = questsRoot;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(staticModel.InfoType), staticModel.InfoType, null);
            }

            var staticUI = Instantiate(CompassStaticHUDModel.Instance.StaticPrefab, root)
                .GetComponent<CompassStaticUIView>();

            switch (staticModel.InfoType)
            {
                case StaticModel.Type.Airdrop:
                    staticUI.BindIcon(airdrop);
                    break;
                case StaticModel.Type.Exfiltration:
                case StaticModel.Type.Switch:
                    staticUI.BindIcon(exfiltration);
                    break;
                case StaticModel.Type.Transit:
                    staticUI.BindIcon(TransitIconLoader.Icon);
                    break;
                case StaticModel.Type.ConditionLeaveItemAtLocation:
                case StaticModel.Type.ConditionPlaceBeacon:
                case StaticModel.Type.ConditionFindItem:
                case StaticModel.Type.ConditionVisitPlace:
                case StaticModel.Type.ConditionInZone:
                    BindQuestMarkerIcon(staticUI, staticModel, false);
                    break;
                case StaticModel.Type.VisitZone:
                    // Story zones: the chapter icon wins when VisitAPI knows the chapter; the
                    // trader avatar stays as the interim/fallback (the 4.x behaviour).
                    BindQuestMarkerIcon(staticUI, staticModel, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(staticModel.InfoType), staticModel.InfoType, null);
            }

            staticUI.storyNameKey = staticModel.StoryNameKey;
            staticUI.storyContent = staticModel.InfoType == StaticModel.Type.VisitZone ||
                                    staticModel.StoryNameKey != null ||
                                    staticModel.TraderId == VisitChapterApi.PlayerTraderId;
            staticUI.nameFallback = !string.IsNullOrEmpty(staticModel.NameFallback)
                ? staticModel.NameFallback
                : staticUI.storyContent ? "Story area" : null;
            staticUI.where = staticModel.Where;
            staticUI.target = staticModel.Target;
            staticUI.nameKey = staticModel.NameKey;
            staticUI.isNotNecessary = staticModel.IsNotNecessary;
            staticUI.descriptionKey = staticModel.DescriptionKey;
            staticUI.InfoType = staticModel.InfoType;
            staticUI.Requirements = staticModel.Requirements;
            staticUI.zoneId = staticModel.ZoneId;
            staticUI.nameText = staticModel.NameText;
            staticUI.descriptionText = staticModel.DescriptionText;

            return staticUI;
        }

        /// <summary>
        ///     Binds the icon of a quest/objective marker.
        ///
        ///     Story markers (VisitAPI zones and quests owned by the Player Trader placeholder) use
        ///     the icon of the story chapter their quest belongs to - the glyph the Main Quests
        ///     screen shows. VisitAPI downloads it on demand; until it is cached the trader avatar is
        ///     bound as the interim, and the chapter icon replaces it once
        ///     <see cref="ResolvePendingChapterIcons" /> sees it (a late avatar callback cannot
        ///     overwrite a bound chapter icon - HasStoryIcon).
        /// </summary>
        private static void BindQuestMarkerIcon(CompassStaticUIView staticUI, StaticModel staticModel,
            bool storyZone)
        {
            var settingsModel = SettingsModel.Instance;
            var showTraderIcon = settingsModel.KeyCompassStaticTraderIcon.Value;
            var showStoryIcon = settingsModel.KeyCompassStaticStoryIcon.Value;

            var hasTrader = !string.IsNullOrEmpty(staticModel.TraderId);

            // A resolved chapter marks a story task even when a regular trader gives it. Its
            // marker uses the chapter icon instead of the trader avatar, and the duplicate
            // VisitAPI zone marker is skipped by the controller.
            var storyContent = storyZone || staticModel.StoryNameKey != null || !hasTrader ||
                               staticModel.TraderId == VisitChapterApi.PlayerTraderId;

            if (storyContent && showStoryIcon && !string.IsNullOrEmpty(staticModel.ChapterIconUrl))
            {
                var chapterSprite = VisitChapterApi.GetCachedSprite(staticModel.ChapterIconUrl);

                if (chapterSprite != null)
                {
                    staticUI.HasStoryIcon = true;

                    staticUI.BindIcon(chapterSprite);

                    return;
                }

                VisitChapterApi.PreloadSprite(staticModel.ChapterIconUrl);

                PendingChapterIcons.Add(new PendingChapterIcon
                {
                    View = staticUI,
                    Url = staticModel.ChapterIconUrl,
                    Frames = 600
                });
            }

            if (!showTraderIcon)
                return;

            if (!hasTrader)
                return;

            _TraderSettingsHelper.BindAvatar(staticModel.TraderId, sprite =>
            {
                if (staticUI == null || staticUI.HasStoryIcon)
                    return;

                if (sprite != null)
                    staticUI.BindIcon(sprite);
            });
        }

        /// <summary>
        ///     Gives the queued chapter icons time to arrive (VisitAPI downloads them through the
        ///     SPT server; the marker keeps the trader avatar until then, or forever when the icon
        ///     never resolves).
        /// </summary>
        private static void ResolvePendingChapterIcons()
        {
            if (PendingChapterIcons.Count == 0)
                return;

            if (--_chapterIconCooldown > 0)
                return;

            _chapterIconCooldown = 15;

            for (var i = PendingChapterIcons.Count - 1; i >= 0; i--)
            {
                var pending = PendingChapterIcons[i];

                if (pending.View == null)
                {
                    PendingChapterIcons.RemoveAt(i);

                    continue;
                }

                var sprite = VisitChapterApi.GetCachedSprite(pending.Url);

                if (sprite != null)
                {
                    pending.View.HasStoryIcon = true;

                    pending.View.BindIcon(sprite);

                    PendingChapterIcons.RemoveAt(i);

                    continue;
                }

                pending.Frames -= 15;

                if (pending.Frames > 0)
                    continue;

                // Keep the trader avatar - the chapter icon is not available this session.
                PendingChapterIcons.RemoveAt(i);
            }
        }

        /// <summary>
        ///     Localises a key, returning null when the game has no entry for it. EFT's localizer
        ///     echoes the key back for missing entries; both cases mean "no text".
        /// </summary>
        private static string LocalizedOrNull(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var value = _LocalizedHelper.Localized(key);

            return string.IsNullOrEmpty(value) || value == key ? null : value;
        }

        /// <summary>
        ///     Marker name: the quest's own localisation, else the name of the story chapter it
        ///     belongs to (hidden story objectives have no name of their own).
        /// </summary>
        private static string ResolveName(CompassStaticUIView ui)
        {
            if (!ui.storyContent)
                return _LocalizedHelper.Localized(ui.nameKey) ?? string.Empty;

            // Story markers: quest name -> chapter name -> area label. The game echoes the key
            // back for entries it does not have (hidden story objectives use "<id> name" keys
            // that do not exist), so both lookups go through LocalizedOrNull: a raw id can never
            // reach the UI.
            var name = LocalizedOrNull(ui.nameKey) ?? LocalizedOrNull(ui.storyNameKey);

            if (name != null)
                return name;

            return ui.nameFallback ?? string.Empty;
        }

        /// <summary>
        ///     Marker description: the localised description, else the "story objective" label for
        ///     story zones (their condition ids are never localised - the raw id used to leak).
        /// </summary>
        private static string ResolveDescription(CompassStaticUIView ui)
        {
            if (!ui.storyContent)
                return _LocalizedHelper.Localized(ui.descriptionKey) ?? string.Empty;

            // The condition id has no localisation for story objectives; LocalizedOrNull suppresses
            // the echoed key so the panel never shows a raw condition id.
            var description = LocalizedOrNull(ui.descriptionKey);

            return description ?? StoryObjectiveText;
        }

        private static void DestroyAll(GameWorld __instance)
        {
            foreach (var id in CompassStatics.Keys)
            {
                RemoveStaticUI(id);
            }

            PendingChapterIcons.Clear();
        }

        private static void DestroyStaticUI(string id)
        {
            if (CompassStatics.ContainsKey(id))
            {
                RemoveStaticUI(id);
            }
        }

        private static void RemoveStaticUI(string id)
        {
            if (!Removes.Contains(id))
            {
                Removes.Add(id);
            }
        }

#endif
    }
}