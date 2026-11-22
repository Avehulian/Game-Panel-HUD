#if !UNITY_EDITOR

using EFT.Quests;
using GamePanelHUDCompass.Models;

namespace GamePanelHUDCompass
{
    public partial class GamePanelHUDCompassPlugin
    {
        private static void OnConditionValueChanged(EQuestStatus status,
            Condition condition)
        {
            // `status` is the group the changed condition belongs to, not the quest status:
            // objectives live in AvailableForFinish, so testing "status != Started" removed the
            // marker on every counter change - including the counter reset the game performs
            // when a failed task is accepted again. Only a terminal group means the task itself
            // finished or failed; those are the only cases that should clear the marker.
            switch (status)
            {
                case EQuestStatus.Success:
                case EQuestStatus.Fail:
                case EQuestStatus.FailRestartable:
                case EQuestStatus.MarkedAsFailed:
                case EQuestStatus.Expired:
                    CompassStaticHUDModel.Instance.DestroyStatic?.Invoke(condition.id.ToString());
                    break;
            }

            // VisitAPI 剧情区域标记的 id 形如 "visit:zoneId:conditionId"，
            // 任务状态一变就让下一轮对账重建，避免已完成的区域继续挂在罗盘上。
            CompassStaticHUDModel.Instance.VisitZonesDirty = true;
        }
    }
}

#endif
