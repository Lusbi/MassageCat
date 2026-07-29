using System;
using System.Collections.Generic;
using GameCore.Database;

namespace MassageCat.Gallery
{
    internal enum GallerySortMode
    {
        RoleOrder,
        MissingCardFirst,
        TomatoIncompleteFirst,
    }

    internal sealed class GalleryRoleEntry
    {
        internal RoleData RoleData;
        internal ScenemapData SceneData;
        internal bool CardReward;
        internal bool TomatoClear;
    }

    internal sealed class GalleryCatalogModel
    {
        private static readonly Comparison<GalleryRoleEntry> s_roleOrderComparison = CompareRoleOrder;
        private static readonly Comparison<GalleryRoleEntry> s_missingCardComparison = CompareMissingCard;
        private static readonly Comparison<GalleryRoleEntry> s_tomatoIncompleteComparison = CompareTomatoIncomplete;

        private readonly List<GalleryRoleEntry> m_allEntries = new List<GalleryRoleEntry>(64);
        private readonly List<GalleryRoleEntry> m_visibleEntries = new List<GalleryRoleEntry>(64);
        private readonly Dictionary<string, GalleryRoleState> m_stateByRoleKey = new Dictionary<string, GalleryRoleState>(64);

        internal IReadOnlyList<GalleryRoleEntry> VisibleEntries => m_visibleEntries;

        internal void ApplySnapshot(
            GalleryStateSnapshot snapshot,
            GallerySortMode sortMode,
            string sceneKey,
            bool missingCardOnly,
            bool tomatoIncompleteOnly)
        {
            m_stateByRoleKey.Clear();
            if (snapshot?.roles != null)
            {
                for (int i = 0; i < snapshot.roles.Length; i++)
                {
                    GalleryRoleState state = snapshot.roles[i];
                    if (state == null || string.IsNullOrEmpty(state.roleKey))
                        continue;

                    m_stateByRoleKey[state.roleKey] = state;
                }
            }

            RebuildAllEntries();
            ApplyView(sortMode, sceneKey, missingCardOnly, tomatoIncompleteOnly);
        }

        internal void ApplyView(
            GallerySortMode sortMode,
            string sceneKey,
            bool missingCardOnly,
            bool tomatoIncompleteOnly)
        {
            m_visibleEntries.Clear();
            for (int i = 0; i < m_allEntries.Count; i++)
            {
                GalleryRoleEntry entry = m_allEntries[i];
                if (string.IsNullOrEmpty(sceneKey) == false
                    && string.Equals(entry.RoleData.SceneReference.GetKey(), sceneKey, StringComparison.Ordinal) == false)
                {
                    continue;
                }

                if (missingCardOnly && entry.CardReward)
                    continue;

                if (tomatoIncompleteOnly && entry.TomatoClear)
                    continue;

                m_visibleEntries.Add(entry);
            }

            switch (sortMode)
            {
                case GallerySortMode.MissingCardFirst:
                    m_visibleEntries.Sort(s_missingCardComparison);
                    break;
                case GallerySortMode.TomatoIncompleteFirst:
                    m_visibleEntries.Sort(s_tomatoIncompleteComparison);
                    break;
                default:
                    m_visibleEntries.Sort(s_roleOrderComparison);
                    break;
            }
        }

        private void RebuildAllEntries()
        {
            m_allEntries.Clear();
            Dictionary<string, RoleData>.ValueCollection roleDatas = Database<RoleData>.GetAll();
            if (roleDatas == null)
                return;

            foreach (RoleData roleData in roleDatas)
            {
                if (roleData == null
                    || m_stateByRoleKey.TryGetValue(roleData.key, out GalleryRoleState state) == false
                    || state.unlocked == false)
                {
                    continue;
                }

                roleData.SceneReference.TryLoad(out ScenemapData sceneData);
                m_allEntries.Add(new GalleryRoleEntry
                {
                    RoleData = roleData,
                    SceneData = sceneData,
                    CardReward = state.cardReward,
                    TomatoClear = state.tomatoClear,
                });
            }
        }

        private static int CompareRoleOrder(GalleryRoleEntry left, GalleryRoleEntry right)
        {
            return left.RoleData.roleSortId.CompareTo(right.RoleData.roleSortId);
        }

        private static int CompareMissingCard(GalleryRoleEntry left, GalleryRoleEntry right)
        {
            int result = left.CardReward.CompareTo(right.CardReward);
            return result != 0 ? result : CompareRoleOrder(left, right);
        }

        private static int CompareTomatoIncomplete(GalleryRoleEntry left, GalleryRoleEntry right)
        {
            int result = left.TomatoClear.CompareTo(right.TomatoClear);
            return result != 0 ? result : CompareRoleOrder(left, right);
        }
    }
}
