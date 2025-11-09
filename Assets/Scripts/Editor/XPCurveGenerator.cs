using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Expo.Editor
{
    /// <summary>
    /// Editor utility to generate exponential XP curves for progression configuration
    /// </summary>
    public static class XPCurveGenerator
    {
        [MenuItem("Expo/Generate XP Curve")]
        public static void GenerateXPCurve()
        {
            // Configuration
            int maxLevel = 10;
            int baseXP = 100;
            float exponentialFactor = 1.5f; // 1.5 = 50% increase per level
            
            Debug.Log("=== XP Curve Generator ===");
            Debug.Log($"Base XP: {baseXP}, Exponential Factor: {exponentialFactor}");
            Debug.Log("Copy this into your progression_config.json:\n");
            
            List<int> xpValues = new List<int>();
            
            for (int level = 1; level <= maxLevel; level++)
            {
                int xpRequired;
                
                if (level == 1)
                {
                    xpRequired = 0; // Level 1 starts at 0 XP
                }
                else
                {
                    // Exponential formula: XP = baseXP * (exponentialFactor ^ (level - 2))
                    // Cumulative total for each level
                    int previousTotal = level > 2 ? xpValues[level - 2] : 0;
                    int xpForThisLevel = Mathf.RoundToInt(baseXP * Mathf.Pow(exponentialFactor, level - 2));
                    xpRequired = previousTotal + xpForThisLevel;
                }
                
                xpValues.Add(xpRequired);
                
                Debug.Log($"Level {level}: {xpRequired} XP total" + 
                         (level > 1 ? $" (+{xpRequired - xpValues[level - 2]} XP from previous)" : ""));
            }
            
            Debug.Log("\n=== JSON Format ===");
            Debug.Log("Use these values in your progression_config.json:");
            for (int i = 0; i < xpValues.Count; i++)
            {
                Debug.Log($"  Level {i + 1}: \"xpRequired\": {xpValues[i]},");
            }
        }
        
        [MenuItem("Expo/Generate Custom XP Curve")]
        public static void ShowXPCurveWindow()
        {
            XPCurveGeneratorWindow.ShowWindow();
        }
    }
    
    /// <summary>
    /// Editor window for customizing XP curve generation
    /// </summary>
    public class XPCurveGeneratorWindow : EditorWindow
    {
        private int maxLevel = 10;
        private int baseXP = 100;
        private float exponentialFactor = 1.5f;
        private Vector2 scrollPosition;
        
        public static void ShowWindow()
        {
            GetWindow<XPCurveGeneratorWindow>("XP Curve Generator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("XP Curve Configuration", EditorStyles.boldLabel);
            
            maxLevel = EditorGUILayout.IntField("Max Level", maxLevel);
            baseXP = EditorGUILayout.IntField("Base XP (Level 2)", baseXP);
            exponentialFactor = EditorGUILayout.Slider("Growth Factor", exponentialFactor, 1.1f, 3.0f);
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Growth Factor:\n" +
                "1.1 = Very gradual (10% increase per level)\n" +
                "1.5 = Moderate (50% increase per level)\n" +
                "2.0 = Steep (100% increase per level)\n" +
                "2.5 = Very steep (150% increase per level)",
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Curve", GUILayout.Height(30)))
            {
                GenerateCurve();
            }
            
            EditorGUILayout.Space();
            GUILayout.Label("Preview:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            List<int> previewValues = CalculateXPCurve();
            for (int i = 0; i < previewValues.Count; i++)
            {
                int level = i + 1;
                int xp = previewValues[i];
                int xpGain = i > 0 ? xp - previewValues[i - 1] : 0;
                
                string label = $"Level {level}: {xp:N0} XP";
                if (xpGain > 0)
                {
                    label += $" (+{xpGain:N0})";
                }
                
                EditorGUILayout.LabelField(label);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private List<int> CalculateXPCurve()
        {
            List<int> xpValues = new List<int>();
            
            for (int level = 1; level <= maxLevel; level++)
            {
                int xpRequired;
                
                if (level == 1)
                {
                    xpRequired = 0;
                }
                else
                {
                    int previousTotal = level > 2 ? xpValues[level - 2] : 0;
                    int xpForThisLevel = Mathf.RoundToInt(baseXP * Mathf.Pow(exponentialFactor, level - 2));
                    xpRequired = previousTotal + xpForThisLevel;
                }
                
                xpValues.Add(xpRequired);
            }
            
            return xpValues;
        }
        
        private void GenerateCurve()
        {
            List<int> xpValues = CalculateXPCurve();
            
            Debug.Log("=== Generated XP Curve ===");
            Debug.Log($"Configuration: Base XP = {baseXP}, Growth Factor = {exponentialFactor}, Max Level = {maxLevel}");
            Debug.Log("\nCopy these values into your progression_config.json:\n");
            
            for (int i = 0; i < xpValues.Count; i++)
            {
                int level = i + 1;
                int xp = xpValues[i];
                int xpGain = i > 0 ? xp - xpValues[i - 1] : 0;
                
                Debug.Log($"Level {level}: \"xpRequired\": {xp}," + 
                         (xpGain > 0 ? $"  // +{xpGain} XP from previous level" : ""));
            }
            
            Debug.Log("\n=== End Generated Curve ===");
        }
    }
}