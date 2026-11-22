using System;
using System.Collections.Generic;
using EFT.Quests;
using GamePanelHUDCompass.Models;
using UnityEngine;

namespace GamePanelHUDCompass.VisitApi
{
    /// <summary>
    /// 把 VisitAPI 区域表翻译成 Compass 的 StaticModel。
    ///
    /// 与 VisitAPI 自己的做法一致：区域本身不带任务归属，归属来自任务条件里的 zoneId。
    /// 因此这里反向扫一遍「进行中任务」的条件，把 zoneId 与区域表的 id 对上，
    /// 顺带拿到任务的 TraderId —— Compass 的静态标记就是靠它绑商人头像的。
    /// </summary>
    internal static class VisitZoneStaticBuilder
    {
        /// <summary>
        /// 收集当前战局应当显示的剧情区域标记。
        /// </summary>
        /// <param name="zones">VisitAPI 区域表</param>
        /// <param name="locationId">当前地图 id（GameWorld.LocationId）</param>
        /// <param name="gameTime">游戏内时间，用于时段过滤；null 表示不做时段过滤</param>
        /// <param name="quests">玩家任务簿（QuestBook 即 BindableList&lt;Quest&gt;）</param>
        /// <param name="conditionsOf">取任务全部条件的回调（含 counterCreator 展开）</param>
        public static List<StaticModel> Build(
            List<VisitZone> zones,
            string locationId,
            DateTime? gameTime,
            IEnumerable<Quest> quests,
            Func<Quest, IEnumerable<object>> conditionsOf)
        {
            var result = new List<StaticModel>();

            if (zones == null || zones.Count == 0 || string.IsNullOrEmpty(locationId) || quests == null)
                return result;

            var zonesById = new Dictionary<string, List<VisitZone>>(StringComparer.Ordinal);

            foreach (var zone in zones)
            {
                if (string.IsNullOrEmpty(zone.Id) || !zone.CoversLocation(locationId))
                    continue;

                if (!zonesById.TryGetValue(zone.Id, out var list))
                {
                    list = new List<VisitZone>(1);
                    zonesById.Add(zone.Id, list);
                }

                list.Add(zone);
            }

            if (zonesById.Count == 0)
                return result;

            // The chapter graph (quest -> requirements) is stable for the session; VisitAPI's
            // chapter flags map the direct story objectives, the graph covers the deeper ones.
            VisitChapterResolver.EnsureBuilt(quests);

            foreach (var quest in quests)
            {
                if (quest == null)
                    continue;

                // AvailableForFinish is still an active task: its objectives stay live until the
                // task is handed in (and a restarted failed task can land there immediately).
                if (quest.QuestStatus != EQuestStatus.Started &&
                    quest.QuestStatus != EQuestStatus.AvailableForFinish)
                    continue;

                var template = quest.Template;

                if (template == null)
                    continue;

                // NameLocaleKey is the actual localisation key ("<id> name" when the quest has no
                // custom name). Template.Name is already localised, so re-localising it echoed
                // raw ids into the marker panel when the key had no entry.
                var nameKey = template.NameLocaleKey;

                if (string.IsNullOrEmpty(nameKey))
                    nameKey = template.Id + " name";

                var traderId = template.TraderId;

                // Story chapter: its name is the marker-name fallback for hidden objectives, its
                // icon (VisitAPI quest flags) is the glyph the Main Quests screen shows.
                string storyNameKey = null;
                string chapterIconUrl = null;

                if (VisitChapterResolver.TryResolve(template.Id, out var chapterId, out storyNameKey))
                {
                    chapterIconUrl = VisitChapterApi.GetIconUrl(chapterId);
                }

                IEnumerable<object> conditions;

                try
                {
                    conditions = conditionsOf(quest);
                }
                catch (Exception)
                {
                    continue;
                }

                if (conditions == null)
                    continue;

                foreach (var condition in conditions)
                {
                    Collect(condition, zonesById, nameKey, storyNameKey, chapterIconUrl, traderId, gameTime,
                        result);
                }
            }

            return result;
        }

        private static void Collect(
            object condition,
            Dictionary<string, List<VisitZone>> zonesById,
            string nameKey,
            string storyNameKey,
            string chapterIconUrl,
            string traderId,
            DateTime? gameTime,
            List<StaticModel> result)
        {
            switch (condition)
            {
                case ConditionLeaveItemAtLocation leave:
                    Emit(leave.zoneId, leave.id, !leave.IsNecessary, nameKey, storyNameKey, chapterIconUrl, traderId,
                        gameTime, zonesById, result);
                    break;

                case ConditionPlaceBeacon beacon:
                    Emit(beacon.zoneId, beacon.id, !beacon.IsNecessary, nameKey, storyNameKey, chapterIconUrl,
                        traderId, gameTime, zonesById, result);
                    break;

                case ConditionVisitPlace visit:
                    Emit(visit.target, visit.id, false, nameKey, storyNameKey, chapterIconUrl, traderId, gameTime,
                        zonesById, result);
                    break;

                case ConditionInZone inZone:
                    if (inZone.zoneIds == null)
                        break;

                    foreach (var zoneId in inZone.zoneIds)
                    {
                        Emit(zoneId, inZone.id, false, nameKey, storyNameKey, chapterIconUrl, traderId, gameTime,
                            zonesById, result);
                    }

                    break;
            }
        }

        /// <summary>
        /// 描述行：优先用 VisitAPI 给的交互提示（例如对讲机的「使用」），
        /// 没有则标出区域类型，便于和原生任务条件区分。
        /// </summary>
        private static string BuildDescriptionText(VisitZone zone)
        {
            if (!string.IsNullOrEmpty(zone.InteractPrompt))
                return zone.InteractPrompt;

            return "Story objective";
        }

        private static void Emit(
            string zoneId,
            string conditionId,
            bool isOptional,
            string nameKey,
            string storyNameKey,
            string chapterIconUrl,
            string traderId,
            DateTime? gameTime,
            Dictionary<string, List<VisitZone>> zonesById,
            List<StaticModel> result)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;

            if (!zonesById.TryGetValue(zoneId, out var zones))
                return;

            foreach (var zone in zones)
            {
                if (!zone.HoursOk(gameTime))
                    continue;

                result.Add(new StaticModel
                {
                    Id = "visit:" + zoneId + ":" + conditionId,
                    Where = zone.InteractAt ?? zone.Position,
                    ZoneId = zoneId,
                    NameKey = nameKey,
                    StoryNameKey = storyNameKey,
                    ChapterIconUrl = chapterIconUrl,
                    NameFallback = zone.IsPlaceItem ? "Place area" : "Story area",
                    DescriptionKey = conditionId,
                    TraderId = traderId,
                    IsNotNecessary = isOptional,
                    InfoType = StaticModel.Type.VisitZone,
                    NameText = null,
                    DescriptionText = BuildDescriptionText(zone)
                });
            }
        }


    }
}
