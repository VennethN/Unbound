using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Unbound.Dialogue.Editor
{
    /// <summary>
    /// Custom editor window for creating and managing GifAsset ScriptableObjects
    /// Provides an intuitive interface for setting up GIF animations from sprite sheets
    /// </summary>
    public class GifAssetEditor : EditorWindow
    {
        private GifAsset currentGifAsset;
        private Vector2 scrollPosition;
        private string assetName = "NewGifAsset";

        // Sprite sheet import settings
        private Texture2D spriteSheet;
        private int columns = 1;
        private int rows = 1;
        private Vector2 spriteSize = new Vector2(32, 32);
        private Vector2 spriteOffset = Vector2.zero;
        private List<Sprite> extractedSprites = new List<Sprite>();

        // Preview settings
        private bool showPreview = true;
        private float previewScale = 1f;

        [MenuItem("Window/Unbound/GifAsset Editor")]
        public static void ShowWindow()
        {
            GetWindow<GifAssetEditor>("GifAsset Editor");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("GifAsset Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Asset selection/creation
            DrawAssetSelectionSection();

            if (currentGifAsset != null)
            {
                // Sprite sheet import
                DrawSpriteSheetImportSection();

                // Frame management
                DrawFrameManagementSection();

                // Animation settings
                DrawAnimationSettingsSection();

                // Transition settings
                DrawTransitionSettingsSection();

                // Preview
                if (showPreview)
                {
                    DrawPreviewSection();
                }

                // Save changes
                if (GUILayout.Button("Save Changes"))
                {
                    EditorUtility.SetDirty(currentGifAsset);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Saved changes to {currentGifAsset.name}");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetSelectionSection()
        {
            EditorGUILayout.LabelField("Asset Selection", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            currentGifAsset = (GifAsset)EditorGUILayout.ObjectField("GifAsset", currentGifAsset, typeof(GifAsset), false);

            if (EditorGUI.EndChangeCheck() && currentGifAsset != null)
            {
                assetName = currentGifAsset.name;
                EditorUtility.SetDirty(currentGifAsset);
            }

            EditorGUILayout.BeginHorizontal();
            assetName = EditorGUILayout.TextField("Asset Name", assetName);

            if (GUILayout.Button("Create New"))
            {
                CreateNewGifAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawSpriteSheetImportSection()
        {
            EditorGUILayout.LabelField("Sprite Sheet Import", EditorStyles.boldLabel);

            spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);

            if (spriteSheet != null)
            {
                EditorGUILayout.BeginHorizontal();
                columns = EditorGUILayout.IntField("Columns", columns);
                rows = EditorGUILayout.IntField("Rows", rows);
                EditorGUILayout.EndHorizontal();

                spriteSize = EditorGUILayout.Vector2Field("Sprite Size", spriteSize);
                spriteOffset = EditorGUILayout.Vector2Field("Offset", spriteOffset);

                if (GUILayout.Button("Extract Sprites"))
                {
                    ExtractSpritesFromSheet();
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawFrameManagementSection()
        {
            EditorGUILayout.LabelField("Frame Management", EditorStyles.boldLabel);

            if (currentGifAsset.Frames.Count > 0)
            {
                EditorGUILayout.LabelField($"Frames: {currentGifAsset.Frames.Count}");

                // Frame list with thumbnails
                for (int i = 0; i < currentGifAsset.Frames.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    var sprite = currentGifAsset.Frames[i];
                    if (sprite != null)
                    {
                        var rect = GUILayoutUtility.GetRect(60, 60, GUILayout.Width(60), GUILayout.Height(60));
                        EditorGUI.DrawTextureTransparent(rect, sprite.texture, ScaleMode.ScaleToFit);

                        EditorGUILayout.LabelField($"Frame {i}", GUILayout.Width(60));
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        currentGifAsset.RemoveFrame(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            // Add individual sprites
            var newSprite = (Sprite)EditorGUILayout.ObjectField("Add Sprite", null, typeof(Sprite), false);
            if (newSprite != null)
            {
                currentGifAsset.AddFrame(newSprite);
            }

            EditorGUILayout.Space();
        }

        private void DrawAnimationSettingsSection()
        {
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);

            currentGifAsset.FrameRate = EditorGUILayout.FloatField("Frame Rate (FPS)", currentGifAsset.FrameRate);
            currentGifAsset.Loop = EditorGUILayout.Toggle("Loop", currentGifAsset.Loop);
            currentGifAsset.PlayOnAwake = EditorGUILayout.Toggle("Play on Awake", currentGifAsset.PlayOnAwake);

            EditorGUILayout.Space();
        }

        private void DrawTransitionSettingsSection()
        {
            EditorGUILayout.LabelField("Transition Settings", EditorStyles.boldLabel);

            currentGifAsset.IdleTransition = (GifAsset)EditorGUILayout.ObjectField("Idle Transition", currentGifAsset.IdleTransition, typeof(GifAsset), false);
            currentGifAsset.TalkingTransition = (GifAsset)EditorGUILayout.ObjectField("Talking Transition", currentGifAsset.TalkingTransition, typeof(GifAsset), false);
            currentGifAsset.TransitionSpeed = EditorGUILayout.FloatField("Transition Speed", currentGifAsset.TransitionSpeed);

            EditorGUILayout.Space();
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (currentGifAsset.Frames.Count > 0)
            {
                previewScale = EditorGUILayout.Slider("Preview Scale", previewScale, 0.5f, 3f);

                var previewRect = GUILayoutUtility.GetRect(200 * previewScale, 200 * previewScale);
                if (currentGifAsset.CurrentFrame != null)
                {
                    EditorGUI.DrawTextureTransparent(previewRect, currentGifAsset.CurrentFrame.texture, ScaleMode.ScaleToFit);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play"))
                {
                    currentGifAsset.Play();
                }
                if (GUILayout.Button("Pause"))
                {
                    currentGifAsset.Pause();
                }
                if (GUILayout.Button("Stop"))
                {
                    currentGifAsset.Stop();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Current Frame: {currentGifAsset.GetCurrentFrameIndex() + 1}/{currentGifAsset.FrameCount}");
            }

            EditorGUILayout.Space();
        }

        private void CreateNewGifAsset()
        {
            var asset = ScriptableObject.CreateInstance<GifAsset>();
            asset.name = assetName;

            var path = EditorUtility.SaveFilePanelInProject(
                "Save GifAsset",
                assetName,
                "asset",
                "Create a new GifAsset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                currentGifAsset = asset;
                Debug.Log($"Created new GifAsset at {path}");
            }
        }

        private void ExtractSpritesFromSheet()
        {
            if (spriteSheet == null || columns <= 0 || rows <= 0) return;

            extractedSprites.Clear();

            int totalSprites = columns * rows;
            float spriteWidth = spriteSheet.width / columns;
            float spriteHeight = spriteSheet.height / rows;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var rect = new Rect(
                        x * spriteWidth + spriteOffset.x,
                        spriteSheet.height - (y + 1) * spriteHeight + spriteOffset.y,
                        spriteSize.x,
                        spriteSize.y
                    );

                    var sprite = Sprite.Create(
                        spriteSheet,
                        rect,
                        new Vector2(0.5f, 0.5f),
                        100f
                    );

                    extractedSprites.Add(sprite);
                }
            }

            // Apply extracted sprites to current asset
            if (currentGifAsset != null)
            {
                currentGifAsset.SetFrames(extractedSprites);
                Debug.Log($"Extracted {extractedSprites.Count} sprites from sheet");
            }
        }

        private void OnDestroy()
        {
            // Clean up any temporary sprites we created
            foreach (var sprite in extractedSprites)
            {
                if (sprite != null)
                {
                    DestroyImmediate(sprite);
                }
            }
            extractedSprites.Clear();
        }
    }
}
