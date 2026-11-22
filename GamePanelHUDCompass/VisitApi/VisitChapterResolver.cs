using System;
using System.Collections.Generic;
using EFT.Quests;

namespace GamePanelHUDCompass.VisitApi
{
    /// <summary>
    /// Resolves the story chapter a quest belongs to, using VisitAPI's chapter flags first and the
    /// quest book's own requirement chain as a fallback.
    ///
    /// VisitAPI maps the chapter's direct finish-condition targets only. Some hidden story
    /// objectives hang deeper in the chain (an objective -> hidden quest -> chapter), so the
    /// resolver walks "Quest"-type requirements up to a chapter (and, for chain heads, the
    /// reverse direction through the quests that require them) - the same strategy the SPT 5.0
    /// build used for its chapter icons.
    /// </summary>
    internal static class VisitChapterResolver
    {
        private static Dictionary<string, Quest> _questsById;
        private static Dictionary<string, List<string>> _parents;
        private static Dictionary<string, List<string>> _children;
        private static HashSet<string> _chapters;
        private static object _source;
        private static int _builtCount = -1;

        public static void Reset()
        {
            _questsById = null;
            _parents = null;
            _children = null;
            _chapters = null;
            _source = null;
            _builtCount = -1;
        }

        /// <summary>Builds the quest graph once per quest-book shape (it does not change mid-session).</summary>
        public static void EnsureBuilt(IEnumerable<Quest> quests)
        {
            if (quests == null)
                return;

            // The quest book is the same instance for the whole session - reuse the graph without
            // re-materialising (and re-scanning) ~800 quests on every zone refresh.
            if (_questsById != null && ReferenceEquals(_source, quests))
                return;

            var list = quests as IList<Quest> ?? new List<Quest>(quests);

            if (_questsById != null && _builtCount == list.Count)
            {
                _source = quests;

                return;
            }

            var byId = new Dictionary<string, Quest>(list.Count, StringComparer.Ordinal);

            foreach (var quest in list)
            {
                if (quest == null)
                    continue;

                var template = quest.Template;

                if (template == null || string.IsNullOrEmpty(template.Id))
                    continue;

                byId[template.Id] = quest;
            }

            var parents = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var children = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var pair in byId)
            {
                List<string> requirements;

                try
                {
                    requirements = new List<string>();

                    CollectQuestTargets(pair.Value.NecessaryConditions, requirements);

                    // Conditions outside NecessaryConditions (AvailableForStart/Finish) also carry
                    // the "Quest" requirement links that chain a hidden objective to its chapter.
                    var allConditions = pair.Value.Conditions;

                    if (allConditions != null)
                    {
                        foreach (var entry in allConditions)
                            CollectQuestTargets(entry.Value, requirements);
                    }
                }
                catch (Exception)
                {
                    continue;
                }

                if (requirements.Count == 0)
                    continue;

                parents[pair.Key] = requirements;

                foreach (var requirement in requirements)
                {
                    if (!children.TryGetValue(requirement, out var list2))
                    {
                        list2 = new List<string>();
                        children.Add(requirement, list2);
                    }

                    list2.Add(pair.Key);
                }
            }

            var chapters = new HashSet<string>(StringComparer.Ordinal);

            foreach (var chapterId in VisitChapterApi.GetChapterIds())
            {
                if (!string.IsNullOrEmpty(chapterId))
                    chapters.Add(chapterId);
            }

            if (VisitChapterApi.Available)
            {
                foreach (var pair in byId)
                {
                    if (VisitChapterApi.IsChapter(pair.Key))
                        chapters.Add(pair.Key);
                }
            }

            _questsById = byId;
            _parents = parents;
            _children = children;
            _chapters = chapters;
            _source = quests;
            _builtCount = list.Count;
        }

        /// <summary>
        /// Resolves the chapter (and its name key) for a quest. Returns false when the quest has no
        /// chapter - the marker then keeps the trader avatar.
        /// </summary>
        public static bool TryResolve(string questId, out string chapterId, out string chapterNameKey)
        {
            chapterId = null;
            chapterNameKey = null;

            if (string.IsNullOrEmpty(questId))
                return false;

            if ((_chapters != null && _chapters.Contains(questId)) || VisitChapterApi.IsChapter(questId))
            {
                chapterId = questId;
            }
            else
            {
                chapterId = VisitChapterApi.GetChapterOf(questId);
            }

            if (string.IsNullOrEmpty(chapterId))
                chapterId = Climb(questId, _parents, 8);

            if (string.IsNullOrEmpty(chapterId))
                chapterId = Climb(questId, _children, 5);

            if (string.IsNullOrEmpty(chapterId))
                return false;

            chapterNameKey = NameKeyOf(chapterId);

            return true;
        }

        private static void CollectQuestTargets(IEnumerable<Condition> conditions, List<string> requirements)
        {
            if (conditions == null)
                return;

            foreach (var condition in conditions)
            {
                if (!(condition is ConditionQuest prerequisite))
                    continue;

                var target = prerequisite.target;

                if (!string.IsNullOrEmpty(target) && !requirements.Contains(target))
                    requirements.Add(target);
            }
        }

        /// <summary>The localisation key of the chapter's name, for use when the quest has none.</summary>
        private static string NameKeyOf(string chapterId)
        {
            if (_questsById != null && _questsById.TryGetValue(chapterId, out var chapter))
            {
                // NameLocaleKey is the key to localise later; Template.Name is already localised
                // and re-localising it made the view treat the text as a missing key.
                var localeKey = chapter.Template?.NameLocaleKey;

                if (!string.IsNullOrEmpty(localeKey))
                    return localeKey;
            }

            return chapterId + " name";
        }

        private static string Climb(string questId, Dictionary<string, List<string>> graph, int maxDepth)
        {
            if (graph == null || graph.Count == 0)
                return null;

            var visited = new HashSet<string>(StringComparer.Ordinal) { questId };
            var frontier = new List<string> { questId };

            for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
            {
                var next = new List<string>();

                foreach (var node in frontier)
                {
                    if (!graph.TryGetValue(node, out var links))
                        continue;

                    foreach (var link in links)
                    {
                        if (string.IsNullOrEmpty(link) || !visited.Add(link))
                            continue;

                        if (_chapters != null && _chapters.Contains(link))
                            return link;

                        var mapped = VisitChapterApi.GetChapterOf(link);

                        if (!string.IsNullOrEmpty(mapped))
                            return mapped;

                        next.Add(link);
                    }
                }

                frontier = next;
            }

            return null;
        }
    }
}
