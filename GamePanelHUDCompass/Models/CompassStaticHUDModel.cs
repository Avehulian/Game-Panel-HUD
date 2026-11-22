#if !UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePanelHUDCompass.Models
{
    internal class CompassStaticHUDModel
    {
        private static readonly Lazy<CompassStaticHUDModel> Lazy =
            new Lazy<CompassStaticHUDModel>(() => new CompassStaticHUDModel());

        public static CompassStaticHUDModel Instance => Lazy.Value;

        public bool CompassStaticHUDSw;

        public GameObject StaticPrefab;

        public int AirdropCount;

        public readonly CompassStaticModel CompassStatic = new CompassStaticModel();

        public Action<StaticModel> ShowStatic;

        public Action<string> DestroyStatic;

        // ---- VisitAPI 剧情区域（/visitapi/zones） ----

        /// <summary>本局已创建的区域标记 id 集合，用于增删对账</summary>
        public readonly HashSet<string> VisitZoneShownIds = new HashSet<string>();

        /// <summary>
        /// 任务条件标记已经覆盖的区域 id。
        /// VisitAPI 区域标记会跳过这些区域，避免同一个任务点上同时画出商人头像和章节图标。
        /// </summary>
        public readonly HashSet<string> QuestZoneIds = new HashSet<string>();

        // ---- 地图间转场（TransitController.pointsById） ----

        /// <summary>本局已创建的转场标记 id 集合，用于增删对账</summary>
        public readonly HashSet<string> TransitShownIds = new HashSet<string>();

        /// <summary>上次转场点轮询的时间戳（秒）</summary>
        public float TransitLastRefresh;

        /// <summary>进入战局或任务状态变化时置位，触发一次区域表重建</summary>
        public bool VisitZonesDirty;

        /// <summary>上次重建的时间戳（秒），控制重建频率</summary>
        public float VisitZonesLastRefresh;

        private CompassStaticHUDModel()
        {
        }
    }
}

#endif
