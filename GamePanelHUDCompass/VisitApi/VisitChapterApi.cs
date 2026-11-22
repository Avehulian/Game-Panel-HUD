using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GamePanelHUDCompass.VisitApi
{
    /// <summary>
    /// Soft bridge to VisitAPI's story-chapter data (VisitAPI.Native.QuestFlags and
    /// VisitAPI.ChapterUI.ChapterImages).
    ///
    /// Reflection only on purpose: GamePanelHUD must keep working when VisitAPI is not installed,
    /// and BepInEx 5 gives no load-order guarantee that would make a compile-time reference safe.
    /// Every lookup failure degrades to the previous behaviour (trader avatar, no chapter data).
    ///
    /// VisitAPI's data:
    ///   QuestFlags.ChapterOf(subQuestId) -> chapter quest id (the story chapter it belongs to)
    ///   QuestFlags.IsChapter(id)         -> the quest is itself a chapter
    ///   QuestFlags.Get(id).Icon          -> the chapter icon url (/files/quest/chapters_icon/...)
    ///   ChapterImages.Cached(url)        -> the Sprite once VisitAPI has downloaded it
    ///   ChapterImages.Preload(url)       -> starts that download
    /// </summary>
    internal static class VisitChapterApi
    {
        /// <summary>Placeholder trader used by story/main-quest content ("Player Trader").</summary>
        public const string PlayerTraderId = "67f7af56c117b6140af2a607";

        private static bool _resolved;
        private static bool _available;
        private static bool _failureLogged;

        private static MethodInfo _getEntry;
        private static MethodInfo _chapterOf;
        private static MethodInfo _isChapter;
        private static MethodInfo _chapterIds;
        private static FieldInfo _entryChapter;
        private static FieldInfo _entryIcon;

        private static MethodInfo _imagesCached;
        private static MethodInfo _imagesPreload;

        private static readonly Dictionary<string, string> ChapterOfCache =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static readonly Dictionary<string, bool> ChapterFlagCache =
            new Dictionary<string, bool>(StringComparer.Ordinal);

        private static readonly Dictionary<string, object> EntryCache =
            new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>True when VisitAPI's chapter API could be bound.</summary>
        public static bool Available
        {
            get
            {
                EnsureResolved();

                return _available;
            }
        }

        private static void EnsureResolved()
        {
            if (_resolved)
                return;

            _resolved = true;

            try
            {
                var flags = FindType("VisitAPI.Native.QuestFlags");
                var images = FindType("VisitAPI.ChapterUI.ChapterImages");

                if (flags == null || images == null)
                    return;

                const BindingFlags pub = BindingFlags.Public | BindingFlags.Static;

                _getEntry = flags.GetMethod("Get", pub, null, new[] { typeof(string) }, null);
                _chapterOf = flags.GetMethod("ChapterOf", pub, null, new[] { typeof(string) }, null);
                _isChapter = flags.GetMethod("IsChapter", pub, null, new[] { typeof(string) }, null);
                _chapterIds = flags.GetMethod("ChapterIds", pub, null, Type.EmptyTypes, null);

                var entry = flags.GetNestedType("Entry", BindingFlags.Public);

                if (entry != null)
                {
                    _entryChapter = entry.GetField("Chapter", BindingFlags.Public | BindingFlags.Instance);
                    _entryIcon = entry.GetField("Icon", BindingFlags.Public | BindingFlags.Instance);
                }

                _imagesCached = images.GetMethod("Cached", pub, null, new[] { typeof(string) }, null);
                _imagesPreload = images.GetMethod("Preload", pub, null, new[] { typeof(string) }, null);

                _available = _getEntry != null && _chapterOf != null && _imagesCached != null;
            }
            catch (Exception ex)
            {
                ReportFailure("the VisitAPI chapter bridge could not be bound", ex);
            }
        }

        private static Type FindType(string fullName)
        {
            var type = Type.GetType(fullName + ", VisitAPI");

            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                type = assembly.GetType(fullName, false);

                if (type != null)
                    return type;
            }

            return null;
        }

        private static object GetEntry(string questId)
        {
            if (string.IsNullOrEmpty(questId) || _getEntry == null)
                return null;

            if (EntryCache.TryGetValue(questId, out var cached))
                return cached;

            try
            {
                var entry = _getEntry.Invoke(null, new object[] { questId });

                // Do not cache a miss: VisitAPI may still be fetching its quest flags when the
                // first raid starts, and a cached null would keep the chapter icon away all session.
                if (entry != null)
                    EntryCache[questId] = entry;

                return entry;
            }
            catch (Exception ex)
            {
                ReportFailure($"VisitAPI QuestFlags.Get('{questId}') failed", ex);

                return null;
            }
        }

        /// <summary>The story chapter a quest belongs to, or null when it has none.</summary>
        public static string GetChapterOf(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return null;

            EnsureResolved();

            if (!_available)
                return null;

            if (ChapterOfCache.TryGetValue(questId, out var cached))
                return cached;

            try
            {
                var value = _chapterOf.Invoke(null, new object[] { questId }) as string;

                if (!string.IsNullOrEmpty(value))
                    ChapterOfCache[questId] = value;

                return string.IsNullOrEmpty(value) ? null : value;
            }
            catch (Exception ex)
            {
                ReportFailure($"VisitAPI QuestFlags.ChapterOf('{questId}') failed", ex);

                return null;
            }
        }

        /// <summary>True when the quest is a story chapter itself.</summary>
        public static bool IsChapter(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return false;

            EnsureResolved();

            if (!_available || _isChapter == null)
            {
                var entry = GetEntry(questId);

                return entry != null && _entryChapter != null && (bool)_entryChapter.GetValue(entry);
            }

            if (ChapterFlagCache.TryGetValue(questId, out var cached))
                return cached;

            try
            {
                var value = (bool)_isChapter.Invoke(null, new object[] { questId });

                // Only cache positives for the same reason as GetEntry.
                if (value)
                    ChapterFlagCache[questId] = true;

                return value;
            }
            catch (Exception ex)
            {
                ReportFailure($"VisitAPI QuestFlags.IsChapter('{questId}') failed", ex);

                return false;
            }
        }

        /// <summary>Every chapter id VisitAPI knows (empty when it is not installed/loaded).</summary>
        public static IReadOnlyList<string> GetChapterIds()
        {
            EnsureResolved();

            if (!_available || _chapterIds == null)
                return Array.Empty<string>();

            try
            {
                return _chapterIds.Invoke(null, null) as IReadOnlyList<string> ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                ReportFailure("VisitAPI QuestFlags.ChapterIds() failed", ex);

                return Array.Empty<string>();
            }
        }

        /// <summary>The chapter icon url stored on the VisitAPI quest flags entry.</summary>
        public static string GetIconUrl(string questId)
        {
            var entry = GetEntry(questId);

            if (entry == null || _entryIcon == null)
                return null;

            try
            {
                return _entryIcon.GetValue(entry) as string;
            }
            catch (Exception ex)
            {
                ReportFailure($"VisitAPI icon lookup for '{questId}' failed", ex);

                return null;
            }
        }

        /// <summary>The chapter icon sprite, when VisitAPI has already downloaded and cached it.</summary>
        public static Sprite GetCachedSprite(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            EnsureResolved();

            if (!_available || _imagesCached == null)
                return null;

            try
            {
                return _imagesCached.Invoke(null, new object[] { url }) as Sprite;
            }
            catch (Exception ex)
            {
                ReportFailure($"VisitAPI ChapterImages.Cached('{url}') failed", ex);

                return null;
            }
        }

        /// <summary>Asks VisitAPI to start downloading a chapter icon.</summary>
        public static void PreloadSprite(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            EnsureResolved();

            if (!_available || _imagesPreload == null)
                return;

            try
            {
                _imagesPreload.Invoke(null, new object[] { url });
            }
            catch (Exception ex)
            {
                ReportFailure($"VisitAPI ChapterImages.Preload('{url}') failed", ex);
            }
        }

        /// <summary>Drops per-session caches (quest flags are refetched by VisitAPI itself).</summary>
        public static void Reset()
        {
            ChapterOfCache.Clear();
            ChapterFlagCache.Clear();
            EntryCache.Clear();

            // Retry the binding on the next raid when VisitAPI was not loaded yet (plugins load
            // in arbitrary order; a failed bind is not necessarily permanent).
            if (!_available)
                _resolved = false;
        }

        private static void ReportFailure(string what, Exception ex)
        {
            if (_failureLogged)
                return;

            _failureLogged = true;

            VisitZoneClient.Log?.LogWarning($"[Compass] {what}: {ex.GetBaseException().Message}");
        }
    }
}
