using KSP.Localization;
using ClickThroughFix;
using System;
using UnityEngine;
using UnityEngine.UI;

#if true
namespace EditorExtensionsRedux
{
    public class FineAdjustWindow : MonoBehaviour
    {
        Rect _windowRect = new Rect()
        {
            xMin = Screen.width - 325,
            xMax = Screen.width - 50,
            yMin = 50,
            yMax = 50 //0 height, GUILayout resizes it
        };


        string _windowTitle = string.Empty;

        public static FineAdjustWindow Instance { get; private set; }

        private GameObject _dummyCanvasObj;
        private RectTransform _dummyRect;

        void Awake()
        {
            Log.Debug("FineAdjustWindow Awake()");
            this.enabled = false;
            Instance = this;

            _dummyCanvasObj = new GameObject("EEX_FineAdjust_uGUI_Canvas");
            var canvas = _dummyCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            _dummyCanvasObj.AddComponent<GraphicRaycaster>();

            var panelObj = new GameObject("BlockerPanel");
            panelObj.transform.SetParent(_dummyCanvasObj.transform, false);

            var image = panelObj.AddComponent<Image>();
            image.color = Color.clear;

            _dummyRect = panelObj.GetComponent<RectTransform>();
            _dummyRect.anchorMin = new Vector2(0, 1);
            _dummyRect.anchorMax = new Vector2(0, 1);
            _dummyRect.pivot = new Vector2(0, 1);

            _dummyCanvasObj.SetActive(false);
            DontDestroyOnLoad(_dummyCanvasObj);
        }

        void Start()
        {

        }

        void OnEnable()
        {
            Log.Debug("FineAdjustWindow OnEnable()");
            if (_dummyCanvasObj != null) _dummyCanvasObj.SetActive(true);
        }

        public bool isEnabled()
        {
            return this.enabled;
        }

        void CloseWindow()
        {
            this.enabled = false;
            if (_dummyCanvasObj != null) _dummyCanvasObj.SetActive(false);
            InputLockManager.RemoveControlLock("EEX_FA");
            Log.Info("CloseWindow enabled: " + this.enabled.ToString());
        }

        void OnDisable()
        {
            if (_dummyCanvasObj != null) _dummyCanvasObj.SetActive(false);
            InputLockManager.RemoveControlLock("EEX_FA");
        }

        void OnDestroy()
        {
            if (_dummyCanvasObj != null)
            {
                Destroy(_dummyCanvasObj);
            }
        }

        void OnGUI()
        {
            if (isEnabled())
            {
                _windowTitle = string.Format(Localizer.Format("#LOC_EEX_FineAdjustments"));
                var tstyle = new GUIStyle(GUI.skin.window);

                if (fineAdjustActive)
                {
                    _windowTitle += " " + Localizer.Format("#LOC_EEX_Active");
                    tstyle.normal.textColor = Color.yellow;
                }
                //_windowRect.yMax = _windowRect.yMin;
                _windowRect = ClickThruBlocker.GUILayoutWindow(this.GetInstanceID(), _windowRect, WindowContent, _windowTitle, tstyle);
            }
        }

        enum AdjustmentType
        {
            translation,
            rotation,
        };
        enum CoordinateSystem
        {
            absolute,
            local,
        };

        Part activePuc = null;
        bool fineAdjustActive = false;

        System.Action _pendingGizmoAction = null;

        CoordinateSystem coordSystem = CoordinateSystem.absolute;
        AdjustmentType adjType = AdjustmentType.translation;
        string adjTypeStr = Localizer.Format("#LOC_EEX_Translation");
        public float offset = 0.01f;
        public float rotationZZ = 1.0f;

        const int buttonWidth = 40;
        const int labelWidth = 140;
        const int textfieldWidth = 80;

        //		private string[] _toolbarStrings = { "Translation", "Rotation" };
        //		int toolbarInt = 0;

        void DrawAxisRow(string label, Color color, ref float value, float step, Action<float> applyDelta)
        {
            GUILayout.BeginHorizontal();

            var colorStyle = new GUIStyle(GUI.skin.label);
            colorStyle.normal.textColor = color;

            GUILayout.Label(label, colorStyle, GUILayout.Width(labelWidth));

            if (GUILayout.Button("-", GUILayout.Width(buttonWidth)))
            {
                value -= step;
                applyDelta?.Invoke(-step);
            }

            bool parsed = float.TryParse(GUILayout.TextField(value.ToString("F4"), GUILayout.Width(textfieldWidth)), out float newValue);
            if (parsed && Mathf.Abs(newValue - value) > 0.0001f)
            {
                float delta = newValue - value;
                value = newValue;
                applyDelta?.Invoke(delta);
            }

            if (GUILayout.Button("+", GUILayout.Width(buttonWidth)))
            {
                value += step;
                applyDelta?.Invoke(step);
            }

            GUILayout.EndHorizontal();
        }

        void ApplyViaGizmoOffset(Vector3 axis, float delta)
        {
            if (GizmoEvents.gizmosOffset == null || GizmoEvents.gizmosOffset.Length == 0 || GizmoEvents.gizmosOffset[0] == null)
                return;

            if (GameSettings.VAB_USE_ANGLE_SNAP)
                GameEvents.onEditorSnapModeChange.Fire(false);

            new GIZMOS(EditorExtensions.c, false, true);

            if (GizmoEvents.gizmoOffsetHandle == null)
                return;

            Refl.Invoke(GizmoEvents.gizmosOffset[0], EditorExtensions.c.GIZMOOFFSET_ONHANDLEMOVESTART, GizmoEvents.gizmoOffsetHandle, axis);
            Refl.Invoke(GizmoEvents.gizmosOffset[0], EditorExtensions.c.GIZMOOFFSET_ONHANDLEMOVE,      GizmoEvents.gizmoOffsetHandle, axis, delta);
            Refl.Invoke(GizmoEvents.gizmosOffset[0], EditorExtensions.c.GIZMOOFFSET_ONHANDLEMOVEEND,   GizmoEvents.gizmoOffsetHandle, axis, 0.0f);
        }

        void ApplyViaGizmoRotate(Vector3 axis, float delta)
        {
            if (GizmoEvents.gizmosRotate == null || GizmoEvents.gizmosRotate.Length == 0 || GizmoEvents.gizmosRotate[0] == null)
                return;

            if (GameSettings.VAB_USE_ANGLE_SNAP)
                GameEvents.onEditorSnapModeChange.Fire(false);

            new GIZMOS(EditorExtensions.c, true, false);

            if (GizmoEvents.gizmoRotateHandle == null)
                return;

            Refl.Invoke(GizmoEvents.gizmosRotate[0], EditorExtensions.c.GIZMOROTATE_ONHANDLEROTATESTART, GizmoEvents.gizmoRotateHandle, axis);
            Refl.Invoke(GizmoEvents.gizmosRotate[0], EditorExtensions.c.GIZMOROTATE_ONHANDLEROTATE,      GizmoEvents.gizmoRotateHandle, axis, delta);
            Refl.Invoke(GizmoEvents.gizmosRotate[0], EditorExtensions.c.GIZMOROTATE_ONHANDLEROTATEEND,   GizmoEvents.gizmoRotateHandle, axis, 0.0f);
        }

        void ApplyTransformDelta(AdjustmentType type, CoordinateSystem coordSys, int axisIndex, float delta)
        {
            if (Mathf.Abs(delta) < 0.0001f) return;

            if (!fineAdjustActive)
            {
                fineAdjustActive = true;
                InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS, "EEX_FA");
            }

            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            Vector3 axis = (coordSys == CoordinateSystem.local)
                ? activePuc.transform.rotation * axes[axisIndex]
                : axes[axisIndex];

            if (type == AdjustmentType.translation)
            {
                _pendingGizmoAction = () => ApplyViaGizmoOffset(axis, delta);
            }
            else
            {
                _pendingGizmoAction = () => ApplyViaGizmoRotate(axis, delta);
            }
        }

        void WindowContent(int windowID)
        {
            if (GizmoEvents.rotateGizmoActive)
            {
                adjType = AdjustmentType.rotation;
                if (GizmoEvents.gizmosRotate != null && GizmoEvents.gizmosRotate.Length > 0 && GizmoEvents.gizmosRotate[0] != null)
                {
                    coordSystem = GizmoEvents.gizmosRotate[0].CoordSpace == Space.Self ? CoordinateSystem.local : CoordinateSystem.absolute;
                }
            }
            else if (GizmoEvents.offsetGizmoActive)
            {
                adjType = AdjustmentType.translation;
                if (GizmoEvents.gizmosOffset != null && GizmoEvents.gizmosOffset.Length > 0 && GizmoEvents.gizmosOffset[0] != null)
                {
                    coordSystem = GizmoEvents.gizmosOffset[0].CoordSpace == Space.Self ? CoordinateSystem.local : CoordinateSystem.absolute;
                }
            }

            //GUI.skin = HighLogic.Skin;
            var lstyle = new GUIStyle(GUI.skin.label);
            //var errstyle = new GUIStyle (GUI.skin.label);
            //errstyle.normal.textColor = Color.red;
            if (fineAdjustActive && (DateTime.Now.Second % 2 == 0))
            {
                lstyle.normal.textColor = Color.yellow;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_EEX_CurrentPart"), lstyle, GUILayout.Width(labelWidth));
            var titleStyle = new GUIStyle(lstyle) { wordWrap = true };
            float titleHeight = titleStyle.lineHeight * 2 + titleStyle.padding.vertical; ;
            GUILayout.Label(activePuc ? activePuc.partInfo.title : Localizer.Format("#LOC_EEX_None"), titleStyle, GUILayout.Height(titleHeight));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_EEX_AdjustmentType"), lstyle, GUILayout.Width(labelWidth));
            string currentAdjStr = (adjType == AdjustmentType.translation) ? Localizer.Format("#LOC_EEX_Translation") : Localizer.Format("#LOC_EEX_Rotation");
            GUILayout.Label(currentAdjStr, lstyle);
            GUILayout.EndHorizontal();

            //		if (!GizmoEvents.offsetGizmoActive && !GizmoEvents.rotateGizmoActive)
            //			return;

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_EEX_Coordinates"), lstyle, GUILayout.Width(labelWidth));
            string currentCoordSysStr = (coordSystem == CoordinateSystem.absolute) ? Localizer.Format("#LOC_EEX_CoordinatesAbsolute") : Localizer.Format("#LOC_EEX_CoordinatesLocal");
            GUILayout.Label(currentCoordSysStr, lstyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_EEX_Delta"), lstyle, GUILayout.MinWidth(labelWidth));

            if (GUILayout.Button("/10", GUILayout.Width(buttonWidth)))
            {
                switch (adjType)
                {
                    case AdjustmentType.rotation:
                        rotationZZ /= 10.0f;
                        rotationZZ = Mathf.Clamp((float)Math.Round(rotationZZ, 4), 0.0001f, 1000.0f);
                        break;
                    case AdjustmentType.translation:
                        offset /= 10.0f;
                        offset = Mathf.Clamp((float)Math.Round(offset, 4), 0.0001f, 1000.0f);
                        break;
                }
            }

            switch (adjType)
            {
                case AdjustmentType.rotation:
                {
                    if (float.TryParse(GUILayout.TextField(rotationZZ.ToString("F4"), GUILayout.Width(textfieldWidth)), out float newRotationZZ))
                    {
                        rotationZZ = newRotationZZ;
                    }
                }
                break;
                case AdjustmentType.translation:
                {
                    if (float.TryParse(GUILayout.TextField(offset.ToString("F4"), GUILayout.Width(textfieldWidth)), out float newOffset))
                    {
                        offset = newOffset;
                    }
                    break;
                }
            }

            if (GUILayout.Button("x10", GUILayout.Width(buttonWidth)))
            {
                switch (adjType)
                {
                    case AdjustmentType.rotation:
                        rotationZZ *= 10.0f;
                        rotationZZ = Mathf.Clamp((float)Math.Round(rotationZZ, 4), 0.0001f, 1000.0f);
                        break;
                    case AdjustmentType.translation:
                        offset *= 10.0f;
                        offset = Mathf.Clamp((float)Math.Round(offset, 4), 0.0001f, 1000.0f);
                        break;
                }
            }
            GUILayout.EndHorizontal();

            if (activePuc != null)
            {
                Vector3 currentVal = Vector3.zero;
                switch (adjType)
                {
                    case AdjustmentType.translation:
                    {
                        switch (coordSystem)
                        {
                            case CoordinateSystem.absolute:
                                currentVal = activePuc.transform.position;
                                break;
                            case CoordinateSystem.local:
                                currentVal = Quaternion.Inverse(activePuc.transform.localRotation) * activePuc.transform.localPosition;
                                break;
                        }

                        DrawAxisRow("X", Color.red,   ref currentVal.x, offset, delta => ApplyTransformDelta(adjType, coordSystem, 0, delta));
                        DrawAxisRow("Y", Color.green, ref currentVal.y, offset, delta => ApplyTransformDelta(adjType, coordSystem, 1, delta));
                        DrawAxisRow("Z", Color.blue,  ref currentVal.z, offset, delta => ApplyTransformDelta(adjType, coordSystem, 2, delta));
                    }
                    break;
                    case AdjustmentType.rotation:
                    {
                        switch (coordSystem)
                        {
                            case CoordinateSystem.absolute:
                                currentVal = activePuc.transform.eulerAngles;
                                break;
                            case CoordinateSystem.local:
                                currentVal = activePuc.transform.localEulerAngles;
                                break;
                        }

                        DrawAxisRow("X", Color.red,   ref currentVal.x, rotationZZ, delta => ApplyTransformDelta(adjType, coordSystem, 0, delta));
                        DrawAxisRow("Y", Color.green, ref currentVal.y, rotationZZ, delta => ApplyTransformDelta(adjType, coordSystem, 1, delta));
                        DrawAxisRow("Z", Color.blue,  ref currentVal.z, rotationZZ, delta => ApplyTransformDelta(adjType, coordSystem, 2, delta));
                    }
                    break;
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#LOC_EEX_Done")))
            {
                Log.Info("Done");
                fineAdjustActive = false;
                #region NO_LOCALIZATION
                InputLockManager.RemoveControlLock("EEX_FA");
                #endregion
                CloseWindow();
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        void LateUpdate()
        {
            if (isEnabled() && _dummyRect != null)
            {
                _dummyRect.anchoredPosition = new Vector2(_windowRect.x, -_windowRect.y);
                _dummyRect.sizeDelta = new Vector2(_windowRect.width, _windowRect.height);
            }

            if (_pendingGizmoAction != null)
            {
                var action = _pendingGizmoAction;
                _pendingGizmoAction = null;
                action();
                return;
            }

            if (activePuc != EditorLogic.SelectedPart)
            {
                _windowRect.height = 0;
            }

            activePuc = EditorLogic.SelectedPart;

            #region NO_LOCALIZATION
            if (activePuc == null)
            {
                InputLockManager.RemoveControlLock("EEX_FA");
                return;
            }

            var km = EditorExtensions.Instance.cfg.KeyMap;

            bool anyKey = Input.GetKey(km.Down) || Input.GetKey(km.Up)
                       || Input.GetKey(km.Left) || Input.GetKey(km.Right)
                       || Input.GetKey(km.Forward) || Input.GetKey(km.Back);

            bool isCompoundPart = activePuc is CompoundPart;

            if (anyKey && !fineAdjustActive && !isCompoundPart)
            {
                fineAdjustActive = true;
                InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS, "EEX_FA");
            }

            if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
            {
                bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                       UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

                if (!isPointerOverUI)
                {
                    fineAdjustActive = false;
                    InputLockManager.RemoveControlLock("EEX_FA");
                    return;
                }
            }

            if (!fineAdjustActive || isCompoundPart) return;

            if (adjType == AdjustmentType.translation)
            {
                if (!GizmoEvents.offsetGizmoActive || GizmoEvents.gizmosOffset == null || GizmoEvents.gizmosOffset.Length == 0)
                    return;

                if (GameSettings.VAB_USE_ANGLE_SNAP)
                    GameEvents.onEditorSnapModeChange.Fire(false);

                new GIZMOS(EditorExtensions.c, false, true);

                if (Input.GetKeyDown(km.Down))
                    ApplyViaGizmoOffset(Vector3.down, offset);
                else if (Input.GetKeyDown(km.Up))
                    ApplyViaGizmoOffset(Vector3.up, offset);
                else if (Input.GetKeyDown(km.Left))
                    ApplyViaGizmoOffset(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.forward : Vector3.right, offset);
                else if (Input.GetKeyDown(km.Right))
                    ApplyViaGizmoOffset(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.back : Vector3.left, offset);
                else if (Input.GetKeyDown(km.Forward))
                    ApplyViaGizmoOffset(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.right : Vector3.back, offset);
                else if (Input.GetKeyDown(km.Back))
                    ApplyViaGizmoOffset(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.left : Vector3.forward, offset);
            }
            else
            {
                if (!GizmoEvents.rotateGizmoActive || GizmoEvents.gizmosRotate == null || GizmoEvents.gizmosRotate.Length == 0)
                    return;

                new GIZMOS(EditorExtensions.c, true, false);

                if (Input.GetKeyDown(km.Down))
                    ApplyViaGizmoRotate(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.forward : Vector3.left, rotationZZ);
                else if (Input.GetKeyDown(km.Up))
                    ApplyViaGizmoRotate(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.back : Vector3.right, rotationZZ);
                else if (Input.GetKeyDown(km.Left))
                    ApplyViaGizmoRotate(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.right : Vector3.forward, rotationZZ);
                else if (Input.GetKeyDown(km.Right))
                    ApplyViaGizmoRotate(EditorDriver.editorFacility == EditorFacility.VAB ? Vector3.left : Vector3.back, rotationZZ);
                else if (Input.GetKeyDown(km.Forward))
                    ApplyViaGizmoRotate(Vector3.up, rotationZZ);
                else if (Input.GetKeyDown(km.Back))
                    ApplyViaGizmoRotate(Vector3.down, rotationZZ);
            }
            #endregion
        }

        /// <summary>
        /// Initializes the window content and enables it
        /// </summary>
        public void Show()
        {
            Log.Debug("FineAdjustWindow Show()");
            this.enabled = true;
        }

    }

}

#endif
