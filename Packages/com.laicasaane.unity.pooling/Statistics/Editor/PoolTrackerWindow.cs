using System.Linq;
using UnityEditor;
using UnityEngine;

namespace System.Pooling.Statistics.Editor
{
    public class PoolTrackerWindow : EditorWindow
    {
        static int s_interval;

        static PoolTrackerWindow s_window;

        [MenuItem("Window/Pool Tracker")]
        public static void OpenWindow()
        {
            if (s_window != null)
            {
                s_window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<PoolTrackerWindow>("Pool Tracker").Show();
        }
        
        static readonly GUILayoutOption[] s_emptyLayoutOption = new GUILayoutOption[0];

        PoolTrackerTreeView _treeView;
        object _splitterState;

        void OnEnable()
        {
            s_window = this; // set singleton.
            this._splitterState = SplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);
            this._treeView = new PoolTrackerTreeView();
            PoolTracker.EditorEnableState.EnableAutoReload = EditorPrefs.GetBool(PoolTracker.EnableAutoReloadKey, false);
            PoolTracker.EditorEnableState.EnableTracking = EditorPrefs.GetBool(PoolTracker.EnableTrackingKey, false);
            PoolTracker.EditorEnableState.EnableStackTrace = EditorPrefs.GetBool(PoolTracker.EnableStackTraceKey, false);
        }

        void OnGUI()
        {
            // Head
            RenderHeadPanel();

            // Splittable
            SplitterGUILayout.BeginVerticalSplit(this._splitterState, s_emptyLayoutOption);
            {
                // Column Tabble
                RenderTable();

                // StackTrace details
                RenderDetailsPanel();
            }
            SplitterGUILayout.EndVerticalSplit();
        }

        #region HeadPanel

        public static bool EnableAutoReload => PoolTracker.EditorEnableState.EnableAutoReload;
        public static bool EnableTracking => PoolTracker.EditorEnableState.EnableTracking;
        public static bool EnableStackTrace => PoolTracker.EditorEnableState.EnableStackTrace;
        static readonly GUIContent s_enableAutoReloadHeadContent = EditorGUIUtility.TrTextContent("Enable AutoReload", "Reload automatically.", (Texture)null);
        static readonly GUIContent s_reloadHeadContent = EditorGUIUtility.TrTextContent("Reload", "Reload View.", (Texture)null);
        static readonly GUIContent s_gcHeadContent = EditorGUIUtility.TrTextContent("GC.Collect", "Invoke GC.Collect.", (Texture)null);
        static readonly GUIContent s_enableTrackingHeadContent = EditorGUIUtility.TrTextContent("Enable Tracking", "Start to track async/await UniTask. Performance impact: low", (Texture)null);
        static readonly GUIContent s_enableStackTraceHeadContent = EditorGUIUtility.TrTextContent("Enable StackTrace", "Capture StackTrace when task is started. Performance impact: high", (Texture)null);

        // [Enable Tracking] | [Enable StackTrace]
        void RenderHeadPanel()
        {
            EditorGUILayout.BeginVertical(s_emptyLayoutOption);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, s_emptyLayoutOption);
            
            // search bar
            this._treeView.searchString = EditorGUILayout.TextField(this._treeView.searchString, EditorStyles.toolbarSearchField, s_emptyLayoutOption);

            if (GUILayout.Toggle(EnableAutoReload, s_enableAutoReloadHeadContent, EditorStyles.toolbarButton, s_emptyLayoutOption) != EnableAutoReload)
            {
                PoolTracker.EditorEnableState.EnableAutoReload = !EnableAutoReload;
            }

            if (GUILayout.Toggle(EnableTracking, s_enableTrackingHeadContent, EditorStyles.toolbarButton, s_emptyLayoutOption) != EnableTracking)
            {
                PoolTracker.EditorEnableState.EnableTracking = !EnableTracking;
            }

            if (GUILayout.Toggle(EnableStackTrace, s_enableStackTraceHeadContent, EditorStyles.toolbarButton, s_emptyLayoutOption) != EnableStackTrace)
            {
                PoolTracker.EditorEnableState.EnableStackTrace = !EnableStackTrace;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(s_reloadHeadContent, EditorStyles.toolbarButton, s_emptyLayoutOption))
            {
                PoolTracker.CheckAndResetDirty();
                this._treeView.ReloadAndSort();
                Repaint();
            }

            if (GUILayout.Button(s_gcHeadContent, EditorStyles.toolbarButton, s_emptyLayoutOption))
            {
                GC.Collect(0);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TableColumn

        Vector2 _tableScroll;
        GUIStyle _tableListStyle;

        void RenderTable()
        {
            if (this._tableListStyle == null)
            {
                this._tableListStyle = new GUIStyle("CN Box");
                this._tableListStyle.margin.top = 0;
                this._tableListStyle.padding.left = 3;
            }

            EditorGUILayout.BeginVertical(this._tableListStyle, s_emptyLayoutOption);

            this._tableScroll = EditorGUILayout.BeginScrollView(this._tableScroll, new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.MaxWidth(2000f)
            });
            var controlRect = EditorGUILayout.GetControlRect(new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            });


            this._treeView?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Update()
        {
            if (EnableAutoReload)
            {
                if (s_interval++ % 120 == 0)
                {
                    if (PoolTracker.CheckAndResetDirty())
                    {
                        this._treeView.ReloadAndSort();
                        Repaint();
                    }
                }
            }
        }

        #endregion

        #region Details

        static GUIStyle s_detailsStyle;
        Vector2 _detailsScroll;

        void RenderDetailsPanel()
        {
            if (s_detailsStyle == null)
            {
                s_detailsStyle = new GUIStyle("CN Message");
                s_detailsStyle.wordWrap = false;
                s_detailsStyle.stretchHeight = true;
                s_detailsStyle.margin.right = 15;
            }

            string message = "";
            var selected = this._treeView.state.selectedIDs;
            if (selected.Count > 0)
            {
                var first = selected[0];
                var item = this._treeView.CurrentBindingItems.FirstOrDefault(x => x.id == first) as PoolTrackerViewItem;
                if (item != null)
                {
                    message = item.Position;
                }
            }

            this._detailsScroll = EditorGUILayout.BeginScrollView(this._detailsScroll, s_emptyLayoutOption);
            var vector = s_detailsStyle.CalcSize(new GUIContent(message));
            EditorGUILayout.SelectableLabel(message, s_detailsStyle, new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(vector.x),
                GUILayout.MinHeight(vector.y)
            });
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}