#if !UNITY_EDITOR

using System;
using UnityEngine;

namespace GamePanelHUDCompass.Models
{
    internal struct StaticModel
    {
        public string Id;

        public Vector3 Where;

        public string ZoneId;

        public string[] Target;

        public string NameKey;

        /// <summary>Story chapter name key - the marker name fallback for hidden objectives.</summary>
        public string StoryNameKey;

        /// <summary>Story chapter icon url (VisitAPI quest flags) - the chapter glyph for the marker.</summary>
        public string ChapterIconUrl;

        /// <summary>
        /// Display name used when neither the quest nor its chapter has a localised name
        /// (VisitAPI story zones: "Story area" / "Place area"). Never a raw quest id.
        /// </summary>
        public string NameFallback;

        public string DescriptionKey;

        public string TraderId;

        public bool IsNotNecessary;

        public Type InfoType;

        public Func<bool>[] Requirements;

        /// <summary>
        /// 直接给的显示名——非空时压过 NameKey 的本地化结果。
        /// VisitAPI 区域表里没有可本地化的名称键，只能带原文。
        /// </summary>
        public string NameText;

        /// <summary>直接给的描述——非空时压过 DescriptionKey 的本地化结果。</summary>
        public string DescriptionText;

        public enum Type
        {
            Airdrop,
            Exfiltration,
            Switch,
            ConditionLeaveItemAtLocation,
            ConditionPlaceBeacon,
            ConditionFindItem,
            ConditionVisitPlace,
            ConditionInZone,

            /// <summary>VisitAPI 剧情区域（/visitapi/zones），由任务条件的 zoneId 关联。</summary>
            VisitZone,

            /// <summary>地图间转场（TransitController.pointsById），罗盘上使用 Dynamic Maps 风格的转场图标。</summary>
            Transit
        }
    }
}

#endif
