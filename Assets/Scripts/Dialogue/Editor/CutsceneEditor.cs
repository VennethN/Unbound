using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Unbound.Dialogue.Editor
{
    /// <summary>
    /// Editor window for creating and editing cutscene assets visually
    /// </summary>
    public class CutsceneEditor : EditorWindow
    {
        private CutsceneAsset currentCutscene;
        private Vector2 scrollPosition;
        private Vector2 scrollPositionRight;
        private int selectedStepIndex = -1;
        private int selectedActionIndex = -1;

        // Reorderable lists
        private ReorderableList stepsList;
        private ReorderableList actionsList;

        // GUI styles
        private GUIStyle stepButtonStyle;
        private GUIStyle selectedStepButtonStyle;
        private GUIStyle actionButtonStyle;
        private GUIStyle selectedActionButtonStyle;

        [MenuItem("Window/Unbound/Cutscene Editor")]
        public static void ShowWindow()
        {
            GetWindow<CutsceneEditor>("Cutscene Editor");
        }

        private void OnEnable()
        {
            // Don't setup reorderable lists here - do it in OnGUI when needed
            // This prevents issues with GUI context and null references
        }

        private void OnGUI()
        {
            // Initialize styles on first GUI call to avoid GUI context issues
            if (stepButtonStyle == null)
            {
                InitializeStyles();
            }

            // Ensure reorderable lists are initialized
            if (stepsList == null || actionsList == null)
            {
                SetupReorderableLists();
            }

            EditorGUILayout.BeginHorizontal();

            // Left panel - Cutscene selection and steps
            DrawLeftPanel();

            // Right panel - Step and action editing
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();

            // Bottom panel - Playback controls
            DrawBottomPanel();
        }

        private void InitializeStyles()
        {
            stepButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(2, 2, 2, 2)
            };

            selectedStepButtonStyle = new GUIStyle(stepButtonStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.8f)) },
                fontStyle = FontStyle.Bold
            };

            actionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(15, 10, 3, 3),
                margin = new RectOffset(5, 5, 1, 1),
                fontSize = 10
            };

            selectedActionButtonStyle = new GUIStyle(actionButtonStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(0.3f, 0.6f, 0.9f)) },
                fontStyle = FontStyle.Bold
            };
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private void SetupReorderableLists()
        {
            // Dispose of existing lists to prevent memory leaks and reference issues
            if (stepsList != null)
            {
                stepsList = null;
            }
            if (actionsList != null)
            {
                actionsList = null;
            }

            // Initialize steps list if cutscene doesn't have one
            if (currentCutscene != null && currentCutscene.steps == null)
            {
                currentCutscene.steps = new List<CutsceneStep>();
            }

            // Steps list - use a safe empty list that won't cause issues
            var stepsForList = (currentCutscene?.steps != null) ? currentCutscene.steps : new List<CutsceneStep>();

            // Ensure all steps have proper display names
            if (stepsForList != null)
            {
                for (int i = 0; i < stepsForList.Count; i++)
                {
                    if (stepsForList[i] != null && string.IsNullOrEmpty(stepsForList[i].displayName))
                    {
                        stepsForList[i].displayName = $"Step {i + 1}";
                    }
                }
            }

            stepsList = new ReorderableList(stepsForList, typeof(CutsceneStep), true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    if (currentCutscene == null)
                    {
                        EditorGUI.LabelField(rect, "Cutscene Steps (No Cutscene Selected)");
                    }
                    else
                    {
                        EditorGUI.LabelField(rect, $"Cutscene Steps ({currentCutscene.steps?.Count ?? 0})");
                    }
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    // Extra safety check - if cutscene is null or list is empty, show placeholder
                    if (currentCutscene == null || currentCutscene.steps == null || currentCutscene.steps.Count == 0)
                    {
                        GUI.Label(rect, "No steps available");
                        return;
                    }

                    if (index >= currentCutscene.steps.Count) return;

                    CutsceneStep step = currentCutscene.steps[index];
                    if (step == null) return;

                    GUIStyle style = (index == selectedStepIndex) ? selectedStepButtonStyle : stepButtonStyle;

                    string displayText = step.displayName;
                    if (string.IsNullOrEmpty(displayText))
                    {
                        displayText = $"Step {index + 1}";
                    }

                    if (GUI.Button(rect, $"{index + 1}. {displayText}", style))
                    {
                        selectedStepIndex = index;
                        selectedActionIndex = -1;
                        GUI.changed = true;
                    }
                },

                onAddCallback = (ReorderableList list) =>
                {
                    if (currentCutscene != null && currentCutscene.steps != null)
                    {
                        CutsceneStep newStep = new CutsceneStep
                        {
                            displayName = $"Step {currentCutscene.steps.Count + 1}",
                            duration = 2f,
                            stepID = $"step_{currentCutscene.steps.Count + 1}_{System.Guid.NewGuid().ToString().Substring(0, 8)}"
                        };
                        currentCutscene.steps.Add(newStep);
                        selectedStepIndex = currentCutscene.steps.Count - 1;
                        selectedActionIndex = -1;
                        EditorUtility.SetDirty(currentCutscene);
                    }
                },

                onRemoveCallback = (ReorderableList list) =>
                {
                    if (currentCutscene != null && currentCutscene.steps != null &&
                        selectedStepIndex >= 0 && selectedStepIndex < currentCutscene.steps.Count)
                    {
                        currentCutscene.steps.RemoveAt(selectedStepIndex);
                        selectedStepIndex = Mathf.Min(selectedStepIndex, currentCutscene.steps.Count - 1);
                        EditorUtility.SetDirty(currentCutscene);
                    }
                }
            };

            // Actions list (for selected step) - initialize with empty list for safety
            var emptyActions = new List<CutsceneAction>();
            actionsList = new ReorderableList(emptyActions, typeof(CutsceneAction), true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    if (currentCutscene == null || currentCutscene.steps == null ||
                        selectedStepIndex < 0 || selectedStepIndex >= currentCutscene.steps.Count)
                    {
                        EditorGUI.LabelField(rect, "Step Actions (No Step Selected)");
                    }
                    else
                    {
                        CutsceneStep step = currentCutscene.steps[selectedStepIndex];
                        int actionCount = step.actions?.Count ?? 0;
                        EditorGUI.LabelField(rect, $"Step Actions ({actionCount})");
                    }
                },

                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    // Safety check for actions list
                    if (currentCutscene == null || currentCutscene.steps == null ||
                        selectedStepIndex < 0 || selectedStepIndex >= currentCutscene.steps.Count)
                    {
                        GUI.Label(rect, "No actions available");
                        return;
                    }

                    CutsceneStep step = currentCutscene.steps[selectedStepIndex];
                    if (step == null || step.actions == null || step.actions.Count == 0)
                    {
                        GUI.Label(rect, "No actions in this step");
                        return;
                    }

                    if (index >= step.actions.Count) return;

                    CutsceneAction action = step.actions[index];
                    if (action == null) return;

                    GUIStyle style = (index == selectedActionIndex) ? selectedActionButtonStyle : actionButtonStyle;

                    string actionType = action.GetType().Name.Replace("Action", "");
                    if (GUI.Button(rect, $"{index + 1}. {actionType}: {action.displayName ?? "Unnamed Action"}", style))
                    {
                        selectedActionIndex = index;
                        GUI.changed = true;
                    }
                },

                onAddCallback = (ReorderableList list) =>
                {
                    if (selectedStepIndex >= 0 && currentCutscene != null && currentCutscene.steps != null)
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Move Transform"), false, () => AddAction<MoveTransformAction>());
                        menu.AddItem(new GUIContent("Fade"), false, () => AddAction<FadeAction>());
                        menu.AddItem(new GUIContent("Play Animation"), false, () => AddAction<PlayAnimationAction>());
                        menu.AddItem(new GUIContent("Set Active"), false, () => AddAction<SetActiveAction>());
                        menu.AddItem(new GUIContent("Play Dialogue"), false, () => AddAction<PlayDialogueAction>());
                        menu.AddItem(new GUIContent("Control GIF"), false, () => AddAction<ControlGifAction>());
                        menu.AddItem(new GUIContent("Camera Movement"), false, () => AddAction<CameraMovementAction>());
                        menu.AddItem(new GUIContent("Play Audio"), false, () => AddAction<PlayAudioAction>());
                        menu.AddItem(new GUIContent("Screen Effect"), false, () => AddAction<ScreenEffectAction>());

                        menu.ShowAsContext();
                    }
                },

                onRemoveCallback = (ReorderableList list) =>
                {
                    if (selectedStepIndex >= 0 && selectedActionIndex >= 0 && currentCutscene != null &&
                        currentCutscene.steps != null && selectedStepIndex < currentCutscene.steps.Count)
                    {
                        CutsceneStep step = currentCutscene.steps[selectedStepIndex];
                        if (step.actions != null && selectedActionIndex < step.actions.Count)
                        {
                            step.actions.RemoveAt(selectedActionIndex);
                            selectedActionIndex = Mathf.Min(selectedActionIndex, step.actions.Count - 1);
                            EditorUtility.SetDirty(currentCutscene);
                        }
                    }
                }
            };
        }

        private void AddAction<T>() where T : CutsceneAction, new()
        {
            if (selectedStepIndex < 0 || currentCutscene == null || currentCutscene.steps == null ||
                selectedStepIndex >= currentCutscene.steps.Count) return;

            CutsceneStep step = currentCutscene.steps[selectedStepIndex];
            if (step.actions == null) return;

            T newAction = new T();
            newAction.displayName = $"New {typeof(T).Name.Replace("Action", "")}";

            step.actions.Add(newAction);
            selectedActionIndex = step.actions.Count - 1;
            EditorUtility.SetDirty(currentCutscene);
        }


        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(280), GUILayout.ExpandWidth(true));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            EditorGUILayout.Space(10);

            // Cutscene selection
            EditorGUILayout.LabelField("Cutscene Asset", EditorStyles.boldLabel);

            CutsceneAsset newCutscene = (CutsceneAsset)EditorGUILayout.ObjectField(currentCutscene, typeof(CutsceneAsset), false);
            if (newCutscene != currentCutscene)
            {
                currentCutscene = newCutscene;
                selectedStepIndex = -1;
                selectedActionIndex = -1;

                // Recreate reorderable lists with new cutscene data
                SetupReorderableLists();

                GUI.changed = true;
                Repaint();
            }

            if (currentCutscene == null)
            {
                EditorGUILayout.HelpBox("Select a Cutscene Asset to edit", MessageType.Info);

                // Ensure reorderable lists are properly initialized even with no cutscene
                if (stepsList == null)
                {
                    SetupReorderableLists();
                }

                // Show empty steps list for consistency
                if (stepsList != null)
                {
                    stepsList.DoLayoutList();
                }

                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(10);

            // Cutscene properties
            EditorGUILayout.LabelField("Cutscene Properties", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            currentCutscene.displayName = EditorGUILayout.TextField("Display Name", currentCutscene.displayName);
            currentCutscene.description = EditorGUILayout.TextField("Description", currentCutscene.description);
            currentCutscene.cutsceneID = EditorGUILayout.TextField("Cutscene ID", currentCutscene.cutsceneID);

            EditorGUILayout.Space(5);
            currentCutscene.pauseGameplayDuringCutscene = EditorGUILayout.Toggle("Pause Gameplay", currentCutscene.pauseGameplayDuringCutscene);
            currentCutscene.hideUIDuringCutscene = EditorGUILayout.Toggle("Hide UI", currentCutscene.hideUIDuringCutscene);
            currentCutscene.fadeInDuration = EditorGUILayout.FloatField("Fade In Duration", currentCutscene.fadeInDuration);
            currentCutscene.fadeOutDuration = EditorGUILayout.FloatField("Fade Out Duration", currentCutscene.fadeOutDuration);

            EditorGUILayout.Space(5);
            currentCutscene.backgroundMusic = (AudioClip)EditorGUILayout.ObjectField("Background Music", currentCutscene.backgroundMusic, typeof(AudioClip), false);
            currentCutscene.musicVolume = EditorGUILayout.Slider("Music Volume", currentCutscene.musicVolume, 0f, 1f);
            currentCutscene.loopBackgroundMusic = EditorGUILayout.Toggle("Loop Music", currentCutscene.loopBackgroundMusic);

            EditorGUILayout.Space(5);
            currentCutscene.completionAction = (CutsceneCompletionAction)EditorGUILayout.EnumPopup("Completion Action", currentCutscene.completionAction);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentCutscene);
            }

            EditorGUILayout.Space(10);

            // Steps list
            stepsList.DoLayoutList();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(320), GUILayout.ExpandWidth(true));
            scrollPositionRight = EditorGUILayout.BeginScrollView(scrollPositionRight, GUILayout.ExpandHeight(true));

            if (currentCutscene == null)
            {
                EditorGUILayout.HelpBox("Select a cutscene to edit its steps and actions", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            if (selectedStepIndex < 0 || selectedStepIndex >= currentCutscene.steps.Count)
            {
                EditorGUILayout.HelpBox("Select a step to edit its properties and actions", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            CutsceneStep selectedStep = currentCutscene.steps[selectedStepIndex];

            EditorGUILayout.Space(10);

            // Step properties
            EditorGUILayout.LabelField($"Step {selectedStepIndex + 1}: {selectedStep.displayName}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            selectedStep.displayName = EditorGUILayout.TextField("Step Name", selectedStep.displayName);
            selectedStep.description = EditorGUILayout.TextField("Description", selectedStep.description);

            EditorGUILayout.Space(5);
            selectedStep.duration = EditorGUILayout.FloatField("Duration", selectedStep.duration);
            selectedStep.delay = EditorGUILayout.FloatField("Delay", selectedStep.delay);

            // Camera settings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);

            selectedStep.cameraSettings.useCustomSettings = EditorGUILayout.Toggle("Use Custom Camera", selectedStep.cameraSettings.useCustomSettings);

            if (selectedStep.cameraSettings.useCustomSettings)
            {
                selectedStep.cameraSettings.targetTransform = (Transform)EditorGUILayout.ObjectField("Target Transform", selectedStep.cameraSettings.targetTransform, typeof(Transform), true);
                selectedStep.cameraSettings.offset = EditorGUILayout.Vector3Field("Offset", selectedStep.cameraSettings.offset);
                selectedStep.cameraSettings.fieldOfView = EditorGUILayout.FloatField("Field of View", selectedStep.cameraSettings.fieldOfView);
                selectedStep.cameraSettings.damping = EditorGUILayout.FloatField("Damping", selectedStep.cameraSettings.damping);
            }

            // Audio settings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);

            selectedStep.voiceOverAudio = (AudioClip)EditorGUILayout.ObjectField("Voice Over", selectedStep.voiceOverAudio, typeof(AudioClip), false);
            selectedStep.voiceOverVolume = EditorGUILayout.Slider("Voice Volume", selectedStep.voiceOverVolume, 0f, 1f);
            selectedStep.soundEffect = (AudioClip)EditorGUILayout.ObjectField("Sound Effect", selectedStep.soundEffect, typeof(AudioClip), false);
            selectedStep.soundEffectVolume = EditorGUILayout.Slider("SFX Volume", selectedStep.soundEffectVolume, 0f, 1f);

            // Scene effects
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scene Effects", EditorStyles.boldLabel);

            selectedStep.fadeColor = EditorGUILayout.ColorField("Fade Color", selectedStep.fadeColor);
            selectedStep.fadeAlpha = EditorGUILayout.Slider("Fade Alpha", selectedStep.fadeAlpha, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentCutscene);
            }

            EditorGUILayout.Space(10);

            // Actions list - update the list reference when step changes
            if (actionsList != null)
            {
                if (selectedStep != null && selectedStep.actions != null)
                {
                    actionsList.list = selectedStep.actions;
                }
                else
                {
                    actionsList.list = new List<CutsceneAction>();
                }
                actionsList.DoLayoutList();
            }

            // Action details
            if (selectedActionIndex >= 0 && selectedStep != null && selectedStep.actions != null &&
                selectedActionIndex < selectedStep.actions.Count)
            {
                EditorGUILayout.Space(10);
                DrawActionEditor(selectedStep.actions[selectedActionIndex]);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawActionEditor(CutsceneAction action)
        {
            if (action == null) return;

            EditorGUILayout.LabelField($"Action: {action.GetType().Name.Replace("Action", "")}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            action.displayName = EditorGUILayout.TextField("Action Name", action.displayName);
            action.description = EditorGUILayout.TextArea(action.description, GUILayout.Height(50));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            action.normalizedStartTime = EditorGUILayout.Slider("Start Time (%)", action.normalizedStartTime, 0f, 1f);
            action.normalizedEndTime = EditorGUILayout.Slider("End Time (%)", action.normalizedEndTime, action.normalizedStartTime, 1f);

            // Draw action-specific properties
            DrawActionSpecificProperties(action);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentCutscene);
            }
        }

        private void DrawActionSpecificProperties(CutsceneAction action)
        {
            switch (action)
            {
                case MoveTransformAction moveAction:
                    moveAction.targetTransform = (Transform)EditorGUILayout.ObjectField("Target Transform", moveAction.targetTransform, typeof(Transform), true);
                    moveAction.useLocalCoordinates = EditorGUILayout.Toggle("Use Local Coordinates", moveAction.useLocalCoordinates);
                    moveAction.startPosition = EditorGUILayout.Vector3Field("Start Position", moveAction.startPosition);
                    moveAction.endPosition = EditorGUILayout.Vector3Field("End Position", moveAction.endPosition);
                    moveAction.startRotation = EditorGUILayout.Vector3Field("Start Rotation", moveAction.startRotation);
                    moveAction.endRotation = EditorGUILayout.Vector3Field("End Rotation", moveAction.endRotation);
                    break;

                case FadeAction fadeAction:
                    fadeAction.fadeTargetType = (FadeAction.FadeTargetType)EditorGUILayout.EnumPopup("Fade Target Type", fadeAction.fadeTargetType);
                    fadeAction.fadeTarget = EditorGUILayout.ObjectField("Fade Target", fadeAction.fadeTarget, GetFadeTargetType(fadeAction.fadeTargetType), true);
                    fadeAction.startAlpha = EditorGUILayout.FloatField("Start Alpha", fadeAction.startAlpha);
                    fadeAction.endAlpha = EditorGUILayout.FloatField("End Alpha", fadeAction.endAlpha);
                    break;

                case PlayAnimationAction animAction:
                    animAction.targetAnimator = (Animator)EditorGUILayout.ObjectField("Target Animator", animAction.targetAnimator, typeof(Animator), true);
                    animAction.animationStateName = EditorGUILayout.TextField("Animation State", animAction.animationStateName);
                    animAction.normalizedTransitionTime = EditorGUILayout.FloatField("Transition Time", animAction.normalizedTransitionTime);
                    animAction.layerIndex = EditorGUILayout.IntField("Layer Index", animAction.layerIndex);
                    break;

                case SetActiveAction activeAction:
                    activeAction.targetGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", activeAction.targetGameObject, typeof(GameObject), true);
                    activeAction.activate = EditorGUILayout.Toggle("Activate", activeAction.activate);
                    activeAction.setAtStart = EditorGUILayout.Toggle("Set at Start", activeAction.setAtStart);
                    break;

                case PlayDialogueAction dialogueAction:
                    dialogueAction.dialogueAsset = (DialogueAsset)EditorGUILayout.ObjectField("Dialogue Asset", dialogueAction.dialogueAsset, typeof(DialogueAsset), false);
                    dialogueAction.waitForDialogueCompletion = EditorGUILayout.Toggle("Wait for Completion", dialogueAction.waitForDialogueCompletion);
                    break;

                case ControlGifAction gifAction:
                    gifAction.targetGifPlayer = (GifPlayer)EditorGUILayout.ObjectField("Target GIF Player", gifAction.targetGifPlayer, typeof(GifPlayer), true);
                    gifAction.command = (ControlGifAction.GifCommand)EditorGUILayout.EnumPopup("Command", gifAction.command);
                    if (gifAction.command == ControlGifAction.GifCommand.SetGif)
                    {
                        gifAction.targetGifAsset = (GifAsset)EditorGUILayout.ObjectField("Target GIF Asset", gifAction.targetGifAsset, typeof(GifAsset), false);
                    }
                    break;

                case CameraMovementAction cameraAction:
                    cameraAction.targetCamera = (UnityEngine.Camera)EditorGUILayout.ObjectField("Target Camera", cameraAction.targetCamera, typeof(UnityEngine.Camera), true);
                    cameraAction.followTarget = (Transform)EditorGUILayout.ObjectField("Follow Target", cameraAction.followTarget, typeof(Transform), true);
                    cameraAction.targetPosition = EditorGUILayout.Vector3Field("Target Position", cameraAction.targetPosition);
                    cameraAction.targetRotation = EditorGUILayout.Vector3Field("Target Rotation", cameraAction.targetRotation);
                    cameraAction.fieldOfView = EditorGUILayout.FloatField("Field of View", cameraAction.fieldOfView);
                    break;

                case PlayAudioAction audioAction:
                    audioAction.audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioAction.audioClip, typeof(AudioClip), false);
                    audioAction.audioSource = (AudioSource)EditorGUILayout.ObjectField("Audio Source", audioAction.audioSource, typeof(AudioSource), true);
                    audioAction.audioPosition = EditorGUILayout.Vector3Field("Audio Position", audioAction.audioPosition);
                    audioAction.useSpatialAudio = EditorGUILayout.Toggle("Use Spatial Audio", audioAction.useSpatialAudio);
                    audioAction.volume = EditorGUILayout.Slider("Volume", audioAction.volume, 0f, 1f);
                    audioAction.loop = EditorGUILayout.Toggle("Loop", audioAction.loop);
                    break;

                case ScreenEffectAction effectAction:
                    effectAction.effectMaterial = (Material)EditorGUILayout.ObjectField("Effect Material", effectAction.effectMaterial, typeof(Material), false);
                    effectAction.overlayTexture = (Texture)EditorGUILayout.ObjectField("Overlay Texture", effectAction.overlayTexture, typeof(Texture), false);
                    effectAction.effectIntensity = EditorGUILayout.Slider("Effect Intensity", effectAction.effectIntensity, 0f, 1f);
                    break;
            }
        }

        private System.Type GetFadeTargetType(FadeAction.FadeTargetType fadeType)
        {
            switch (fadeType)
            {
                case FadeAction.FadeTargetType.CanvasGroup: return typeof(CanvasGroup);
                case FadeAction.FadeTargetType.SpriteRenderer: return typeof(SpriteRenderer);
                case FadeAction.FadeTargetType.Image: return typeof(UnityEngine.UI.Image);
                default: return typeof(Object);
            }
        }

        private void DrawBottomPanel()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));

            if (currentCutscene != null)
            {
                if (GUILayout.Button("Save Cutscene", GUILayout.Height(30)))
                {
                    AssetDatabase.SaveAssets();
                    EditorUtility.SetDirty(currentCutscene);
                    Debug.Log($"Cutscene '{currentCutscene.displayName}' saved successfully");
                }

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Validate", GUILayout.Height(30)))
                {
                    ValidateCutscene();
                }

                EditorGUILayout.Space(10);

                float duration = currentCutscene.GetEstimatedDuration();
                EditorGUILayout.LabelField($"Duration: {duration:F1}s", GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ValidateCutscene()
        {
            if (currentCutscene == null) return;

            List<string> errors = new List<string>();

            // Check cutscene validity
            string cutsceneError = currentCutscene.GetValidationErrors();
            if (!string.IsNullOrEmpty(cutsceneError))
            {
                errors.Add($"Cutscene: {cutsceneError}");
            }

            // Check for common issues
            if (currentCutscene.steps.Count == 0)
            {
                errors.Add("Cutscene has no steps");
            }

            foreach (var step in currentCutscene.steps)
            {
                if (step.actions.Count == 0)
                {
                    errors.Add($"Step '{step.displayName}' has no actions");
                }
            }

            if (errors.Count > 0)
            {
                string errorMessage = string.Join("\n", errors);
                EditorUtility.DisplayDialog("Validation Errors", errorMessage, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation", "Cutscene is valid!", "OK");
            }
        }

        private void OnSelectionChange()
        {
            // If a CutsceneAsset is selected in the Project window, switch to it
            if (Selection.activeObject is CutsceneAsset cutsceneAsset)
            {
                currentCutscene = cutsceneAsset;
                selectedStepIndex = -1;
                selectedActionIndex = -1;
                SetupReorderableLists();
                Repaint();
            }
        }
    }
}
