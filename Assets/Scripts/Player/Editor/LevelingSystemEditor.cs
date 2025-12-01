#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unbound.Player;

namespace Unbound.Player.Editor
{
    /// <summary>
    /// Custom editor for LevelingSystem that provides better visualization of exp curves
    /// </summary>
    [CustomEditor(typeof(LevelingSystem))]
    public class LevelingSystemEditor : UnityEditor.Editor
    {
        private SerializedProperty maxLevelProp;
        private SerializedProperty startingLevelProp;
        private SerializedProperty thresholdModeProp;
        private SerializedProperty manualThresholdsProp;
        private SerializedProperty expCurveProp;
        private SerializedProperty curveExpMultiplierProp;
        private SerializedProperty formulaBaseExpProp;
        private SerializedProperty formulaExponentProp;
        private SerializedProperty onLevelUpProp;
        private SerializedProperty onExpGainedProp;
        private SerializedProperty onExpChangedProp;

        private bool showPreview = true;
        private int previewLevels = 10;

        private void OnEnable()
        {
            maxLevelProp = serializedObject.FindProperty("maxLevel");
            startingLevelProp = serializedObject.FindProperty("startingLevel");
            thresholdModeProp = serializedObject.FindProperty("thresholdMode");
            manualThresholdsProp = serializedObject.FindProperty("manualThresholds");
            expCurveProp = serializedObject.FindProperty("expCurve");
            curveExpMultiplierProp = serializedObject.FindProperty("curveExpMultiplier");
            formulaBaseExpProp = serializedObject.FindProperty("formulaBaseExp");
            formulaExponentProp = serializedObject.FindProperty("formulaExponent");
            onLevelUpProp = serializedObject.FindProperty("OnLevelUp");
            onExpGainedProp = serializedObject.FindProperty("OnExpGained");
            onExpChangedProp = serializedObject.FindProperty("OnExpChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            LevelingSystem levelingSystem = (LevelingSystem)target;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Leveling System", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Level Settings
            EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxLevelProp);
            EditorGUILayout.PropertyField(startingLevelProp);

            EditorGUILayout.Space();

            // Threshold Mode
            EditorGUILayout.LabelField("Experience Threshold Mode", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(thresholdModeProp);

            EditorGUILayout.Space();

            // Show relevant properties based on mode
            ExpThresholdMode mode = (ExpThresholdMode)thresholdModeProp.enumValueIndex;

            switch (mode)
            {
                case ExpThresholdMode.Manual:
                    EditorGUILayout.LabelField("Manual Thresholds", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Define experience thresholds for each level manually. " +
                        "Index 0 = Level 2 threshold, Index 1 = Level 3 threshold, etc.",
                        MessageType.Info);
                    EditorGUILayout.PropertyField(manualThresholdsProp, true);
                    break;

                case ExpThresholdMode.Curve:
                    EditorGUILayout.LabelField("Curve-Based Thresholds", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Use an AnimationCurve to define exp thresholds.\n" +
                        "X-axis: Level progress (0 = Level 1, 1 = Max Level)\n" +
                        "Y-axis: Normalized exp (0 to 1, multiplied by Exp Multiplier)",
                        MessageType.Info);

                    // Draw curve with better rect
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Exp Curve", GUILayout.Width(80));
                    expCurveProp.animationCurveValue = EditorGUILayout.CurveField(
                        expCurveProp.animationCurveValue,
                        Color.cyan,
                        new Rect(0, 0, 1, 1),
                        GUILayout.Height(50)
                    );
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(curveExpMultiplierProp, new GUIContent("Max Exp (at Y=1)"));
                    break;

                case ExpThresholdMode.Formula:
                    EditorGUILayout.LabelField("Formula-Based Thresholds", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Exp = BaseExp × (Level ^ Exponent)\n" +
                        "Higher exponent = steeper curve at higher levels",
                        MessageType.Info);
                    EditorGUILayout.PropertyField(formulaBaseExpProp, new GUIContent("Base Exp"));
                    EditorGUILayout.PropertyField(formulaExponentProp, new GUIContent("Exponent"));
                    break;
            }

            EditorGUILayout.Space();

            // Preview Section
            DrawThresholdPreview(levelingSystem, mode);

            EditorGUILayout.Space();

            // Events
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onLevelUpProp);
            EditorGUILayout.PropertyField(onExpGainedProp);
            EditorGUILayout.PropertyField(onExpChangedProp);

            EditorGUILayout.Space();

            // Runtime Info (Play Mode Only)
            if (Application.isPlaying)
            {
                DrawRuntimeInfo(levelingSystem);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawThresholdPreview(LevelingSystem levelingSystem, ExpThresholdMode mode)
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "Threshold Preview", true);

            if (!showPreview) return;

            EditorGUI.indentLevel++;

            previewLevels = EditorGUILayout.IntSlider("Preview Levels", previewLevels, 5, 50);

            EditorGUILayout.Space();

            // Draw preview table
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Level", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("Total EXP Required", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("EXP to Next Level", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // Separator
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            int maxPreview = Mathf.Min(previewLevels, maxLevelProp.intValue);
            int previousExp = 0;

            for (int level = 1; level <= maxPreview; level++)
            {
                int totalExp = CalculateExpForLevel(level, mode, levelingSystem);
                int expToNext = totalExp - previousExp;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(level.ToString(), GUILayout.Width(50));
                GUILayout.Label(totalExp.ToString("N0"), GUILayout.Width(120));
                GUILayout.Label(level == 1 ? "-" : expToNext.ToString("N0"));
                EditorGUILayout.EndHorizontal();

                previousExp = totalExp;
            }

            if (maxPreview < maxLevelProp.intValue)
            {
                EditorGUILayout.LabelField($"... ({maxLevelProp.intValue - maxPreview} more levels)");
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private int CalculateExpForLevel(int level, ExpThresholdMode mode, LevelingSystem levelingSystem)
        {
            if (level <= 1) return 0;

            switch (mode)
            {
                case ExpThresholdMode.Manual:
                    return CalculateManualThreshold(level);

                case ExpThresholdMode.Curve:
                    return CalculateCurveThreshold(level);

                case ExpThresholdMode.Formula:
                    return CalculateFormulaThreshold(level);

                default:
                    return 0;
            }
        }

        private int CalculateManualThreshold(int level)
        {
            if (level <= 1) return 0;

            int index = level - 2;
            if (index >= 0 && index < manualThresholdsProp.arraySize)
            {
                return manualThresholdsProp.GetArrayElementAtIndex(index).intValue;
            }

            // Extrapolate
            if (manualThresholdsProp.arraySize == 0) return level * 100;

            int lastThreshold = manualThresholdsProp.GetArrayElementAtIndex(manualThresholdsProp.arraySize - 1).intValue;
            int secondLastThreshold = manualThresholdsProp.arraySize > 1
                ? manualThresholdsProp.GetArrayElementAtIndex(manualThresholdsProp.arraySize - 2).intValue
                : lastThreshold / 2;

            int difference = lastThreshold - secondLastThreshold;
            int extraLevels = index - (manualThresholdsProp.arraySize - 1);

            return lastThreshold + (difference * extraLevels);
        }

        private int CalculateCurveThreshold(int level)
        {
            if (level <= 1) return 0;

            float normalizedLevel = (float)(level - 1) / (maxLevelProp.intValue - 1);
            float curveValue = expCurveProp.animationCurveValue.Evaluate(normalizedLevel);

            return Mathf.RoundToInt(curveValue * curveExpMultiplierProp.floatValue);
        }

        private int CalculateFormulaThreshold(int level)
        {
            if (level <= 1) return 0;

            return Mathf.RoundToInt(formulaBaseExpProp.floatValue * Mathf.Pow(level, formulaExponentProp.floatValue));
        }

        private void DrawRuntimeInfo(LevelingSystem levelingSystem)
        {
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Current Level: {levelingSystem.CurrentLevel}");
            EditorGUILayout.LabelField($"Current EXP: {levelingSystem.CurrentExp:N0}");
            EditorGUILayout.LabelField($"EXP to Next Level: {levelingSystem.ExpToNextLevel:N0}");

            // Progress bar
            Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, levelingSystem.LevelProgress, 
                $"Progress: {(levelingSystem.LevelProgress * 100f):F1}%");

            EditorGUILayout.Space();

            // Debug buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add 100 EXP"))
            {
                levelingSystem.AddExperience(100);
            }
            if (GUILayout.Button("Add 1000 EXP"))
            {
                levelingSystem.AddExperience(1000);
            }
            if (GUILayout.Button("Level Up"))
            {
                levelingSystem.DebugLevelUp();
            }
            if (GUILayout.Button("Reset"))
            {
                levelingSystem.DebugResetLevel();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
#endif

