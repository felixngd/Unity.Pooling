#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Pooling.Statistics.Editor
{
    public class PoolTrackerViewItem : TreeViewItem
    {
        static readonly Regex s_removeHref = new Regex("<a href.+>(.+)</a>", RegexOptions.Compiled);

        public string PoolType { get; set; }
        public int CountInActive { get; set; }
        public int CountActive { get; set; }
        public PoolObjectType PoolObjectType { get; set; }
        public int MaxItemsRecorded { get; set; }
        
        public string Elapsed { get; set; }

        string _position;

        public string Position
        {
            get { return this._position; }
            set
            {
                this._position = value;
                PositionFirstLine = GetFirstLine(this._position);
            }
        }

        public string PositionFirstLine { get; private set; }

        static string GetFirstLine(string str)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\r' || str[i] == '\n')
                {
                    break;
                }

                sb.Append(str[i]);
            }

            return s_removeHref.Replace(sb.ToString(), "$1");
        }

        public PoolTrackerViewItem(int id) : base(id)
        {
        }
    }

    public class PoolTrackerTreeView : TreeView
    {
        const string SORTED_COLUMN_INDEX_STATE_KEY = "PoolTrackerTreeView_sortedColumnIndex";

        public IReadOnlyList<TreeViewItem> CurrentBindingItems;

        public PoolTrackerTreeView()
            : this(new TreeViewState(),
                new MultiColumnHeader(new MultiColumnHeaderState(new[] {
                    new MultiColumnHeaderState.Column() {headerContent = new GUIContent("Pool Type"), width = 20},
                    new MultiColumnHeaderState.Column() {headerContent = new GUIContent("Count Inactive"), width = 10},
                    new MultiColumnHeaderState.Column() {headerContent = new GUIContent("Count Active"), width = 10},
                    new MultiColumnHeaderState.Column() {headerContent = new GUIContent("Max Recorded"), width = 20},
                    new MultiColumnHeaderState.Column() {headerContent = new GUIContent("Pool Object Type"), width = 10},
                    new MultiColumnHeaderState.Column() {headerContent = new GUIContent("Lifetime"), width = 10},
                    new MultiColumnHeaderState.Column() {headerContent = new GUIContent("Stacktrace")}
                })))
        {
        }

        PoolTrackerTreeView(TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            header.sortingChanged += Header_sortingChanged;

            header.ResizeToFit();
            Reload();

            header.sortedColumnIndex = SessionState.GetInt(SORTED_COLUMN_INDEX_STATE_KEY, 1);
        }

        public void ReloadAndSort()
        {
            var currentSelected = this.state.selectedIDs;
            Reload();
            Header_sortingChanged(this.multiColumnHeader);
            this.state.selectedIDs = currentSelected;
        }

        private void Header_sortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SessionState.SetInt(SORTED_COLUMN_INDEX_STATE_KEY, multiColumnHeader.sortedColumnIndex);
            var index = multiColumnHeader.sortedColumnIndex;
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            var items = rootItem.children.Cast<PoolTrackerViewItem>();

            IOrderedEnumerable<PoolTrackerViewItem> orderedEnumerable;
            switch (index)
            {
                case 0:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.PoolType)
                        : items.OrderByDescending(item => item.PoolType);
                    break;
                case 1:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.CountInActive)
                        : items.OrderByDescending(item => item.CountInActive);
                    break;
                case 2:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.CountActive)
                        : items.OrderByDescending(item => item.CountActive);
                    break;
                case 3:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.MaxItemsRecorded)
                        : items.OrderByDescending(item => item.CountInActive + item.CountActive);
                    break;
                
                case 4:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.PoolObjectType)
                        : items.OrderByDescending(item => item.PoolObjectType);
                    break;
                case 5:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => double.Parse(item.Elapsed))
                        : items.OrderByDescending(item => double.Parse(item.Elapsed));
                    break;

                case 6:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.Position)
                        : items.OrderByDescending(item => item.PositionFirstLine);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            CurrentBindingItems = rootItem.children = orderedEnumerable.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {depth = -1};

            var children = new List<TreeViewItem>();

            PoolTracker.ForEachActivePool(entry => {
                children.Add(new PoolTrackerViewItem(entry.trackingId) {
                    PoolType = entry.formattedType,
                    Elapsed = (DateTime.UtcNow - entry.addTime).TotalSeconds.ToString("00.00"),
                    Position = entry.stackTrace,
                    CountInActive = entry.countInactive,
                    CountActive = entry.weakList.AliveCount,
                    PoolObjectType = entry.poolObjectType,
                    MaxItemsRecorded = entry.maxItemsRecorded
                });
            });

            CurrentBindingItems = children;
            root.children = CurrentBindingItems as List<TreeViewItem>;
            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as PoolTrackerViewItem;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                //mark duplicate poolType as orange
                if (columnIndex == 0)
                {
                    var duplicate = PoolTracker.GetDuplicatePoolType(item.PoolType);
                    if (duplicate.Count > 1)
                    {
                        labelStyle.normal.textColor = new Color(1, 0.5f, 0);
                    }else
                    {
                        labelStyle.normal.textColor = Color.white;
                    }
                }
                switch (columnIndex)
                {
                    case 0:
                        if (item != null)
                        {
                            EditorGUI.LabelField(rect, item.PoolType, labelStyle);
                        }

                        break;
                    case 1:
                        if (item != null)
                        {
                            EditorGUI.LabelField(rect, item.CountInActive.ToString(), labelStyle);
                        }
                        break;
                    case 2:
                        if (item != null)
                        {
                            EditorGUI.LabelField(rect, item.CountActive.ToString(), labelStyle);
                        }
                        break;
                    case 3:
                        if (item != null)
                        {
                            EditorGUI.LabelField(rect, item.MaxItemsRecorded.ToString(), labelStyle);
                        }
                        break;
                    case 4:
                        if (item != null)
                        {
                            EditorGUI.LabelField(rect, item.PoolObjectType.ToString(), labelStyle);
                        }
                        break;
                    
                    case 5:
                        if (item != null)
                        {
                            EditorGUI.LabelField(rect, item.Elapsed, labelStyle);
                        }

                        break;
                    case 6:
                        if (item != null)
                        {
                            EditorGUI.LabelField(rect, item.PositionFirstLine, labelStyle);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }
        
        //search bar
        protected override void SearchChanged(string newSearch)
        {
            if (string.IsNullOrEmpty(newSearch))
            {
                CurrentBindingItems = rootItem.children;
            }
            else
            {
                CurrentBindingItems = rootItem.children.Where(item => {
                    var trackerItem = item as PoolTrackerViewItem;

                    return trackerItem.PoolType.IndexOf(newSearch, StringComparison.OrdinalIgnoreCase) >= 0;
                }).ToList();
            }

            BuildRows(rootItem);
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            var trackerItem = item as PoolTrackerViewItem;
            if (trackerItem == null)
            {
                return false;
            }
            return trackerItem.PoolType.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

    }
}