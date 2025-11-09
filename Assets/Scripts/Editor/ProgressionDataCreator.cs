using UnityEngine;
using UnityEditor;
using Expo.Data;

namespace Expo.Editor
{
    /// <summary>
    /// Editor utility to create sample data for the progression system.
    /// This helps with testing and provides examples of how to set up dishes and cooks.
    /// </summary>
    public static class ProgressionDataCreator
    {
        [MenuItem("Expo/Create Sample Progression Data")]
        public static void CreateSampleData()
        {
            CreateSampleDishes();
            Debug.Log("Sample progression data created! Check the Assets/Data folder.");
        }
        
        private static void CreateSampleDishes()
        {
            // Create directory if it doesn't exist
            string dishPath = "Assets/Data/Dishes";
            if (!AssetDatabase.IsValidFolder(dishPath))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Dishes");
            }
            
            // Sample dishes with varying complexity
            var dishes = new[]
            {
                new { name = "BasicBurger", station = "Grill", pickup = 8f, die = 12f },
                new { name = "SimpleSalad", station = "GardeManger", pickup = 4f, die = 8f },
                new { name = "GrilledChicken", station = "Grill", pickup = 12f, die = 15f },
                new { name = "PastaSpecial", station = "Pasta", pickup = 10f, die = 10f },
                new { name = "SearedSalmon", station = "Saute", pickup = 15f, die = 18f },
                new { name = "CaesarSalad", station = "GardeManger", pickup = 6f, die = 10f },
                new { name = "RisottoPrimavera", station = "Pasta", pickup = 18f, die = 12f },
                new { name = "BeefTenderloin", station = "Grill", pickup = 20f, die = 20f }
            };
            
            foreach (var dish in dishes)
            {
                var dishData = ScriptableObject.CreateInstance<DishData>();
                dishData.dishName = dish.name;
                dishData.station = dish.station;
                dishData.pickupTime = dish.pickup;
                dishData.dieTime = dish.die;
                
                string assetPath = $"{dishPath}/{dish.name}.asset";
                AssetDatabase.CreateAsset(dishData, assetPath);
            }
        }
        
        private static void CreateSampleCooks()
        {
            /*
            // Create directory if it doesn't exist
            string cookPath = "Assets/Data/Cooks";
            if (!AssetDatabase.IsValidFolder(cookPath))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Cooks");
            }
            
            // Sample cooks with different specializations
            var cooks = new[]
            {
                new { 
                    name = "NoviceCook", 
                    desc = "New to the kitchen but eager to learn", 
                    station = "All", 
                    speed = 1.2f, 
                    quality = 0.9f, 
                    burn = 0.15f, 
                    level = 1, 
                    exp = 0,
                    color = new Color(0.8f, 0.8f, 0.8f)
                },
                new { 
                    name = "GrillMaster", 
                    desc = "Specializes in perfectly grilled meats", 
                    station = "Grill", 
                    speed = 0.8f, 
                    quality = 1.3f, 
                    burn = 0.05f, 
                    level = 3, 
                    exp = 500,
                    color = new Color(1f, 0.5f, 0.2f)
                },
                new { 
                    name = "SaladChef", 
                    desc = "Quick and precise with cold preparations", 
                    station = "GardeManger", 
                    speed = 0.7f, 
                    quality = 1.1f, 
                    burn = 0.02f, 
                    level = 2, 
                    exp = 200,
                    color = new Color(0.2f, 0.8f, 0.3f)
                },
                new { 
                    name = "PastaMaestro", 
                    desc = "Italian-trained pasta specialist", 
                    station = "Pasta", 
                    speed = 0.9f, 
                    quality = 1.4f, 
                    burn = 0.08f, 
                    level = 4, 
                    exp = 800,
                    color = new Color(0.9f, 0.9f, 0.2f)
                },
                new { 
                    name = "SauteExpert", 
                    desc = "Master of the saute station", 
                    station = "Saute", 
                    speed = 0.85f, 
                    quality = 1.2f, 
                    burn = 0.1f, 
                    level = 3, 
                    exp = 600,
                    color = new Color(0.7f, 0.3f, 0.9f)
                }
            };
            
            foreach (var cook in cooks)
            {
                var cookData = ScriptableObject.CreateInstance<CookData>();
                cookData.cookName = cook.name;
                cookData.description = cook.desc;
                cookData.preferredStation = cook.station;
                cookData.speedMultiplier = cook.speed;
                cookData.qualityMultiplier = cook.quality;
                cookData.burnChance = cook.burn;
                cookData.levelRequired = cook.level;
                cookData.experienceRequired = cook.exp;
                cookData.themeColor = cook.color;
                
                string assetPath = $"{cookPath}/{cook.name}.asset";
                AssetDatabase.CreateAsset(cookData, assetPath);
            }*/
        }
        
        [MenuItem("Expo/Setup Progression Scene")]
        public static void SetupProgressionScene()
        {
            // This would create a basic progression scene setup
            // For now, just log instructions
            Debug.Log("To set up the progression scene:\n" +
                     "1. Create a new scene called 'ProgressionScene'\n" +
                     "2. Add ProgressionSceneController to an empty GameObject\n" +
                     "3. Add ProgressionManager to an empty GameObject\n" +
                     "4. Create UI canvas with ProgressionSceneUI component\n" +
                     "5. Assign sample dishes and cooks to ProgressionManager");
        }
    }
}