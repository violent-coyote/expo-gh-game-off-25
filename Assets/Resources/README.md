# Resources Folder

This folder contains assets that are loaded at runtime using `Resources.Load()`.

## Why this folder exists

Unity requires assets loaded at runtime via `Resources.Load()` to be in a folder named "Resources". This works the same in both the editor and builds (WebGL, standalone, etc.).

## Contents

- **Data/Dishes/** - All DishData ScriptableObjects for the game
- **Data/progression_config.json** - The progression configuration file

## Working with Dishes

**To create or edit dishes:** Work directly in `Assets/Resources/Data/Dishes/`

1. Right-click in the `Assets/Resources/Data/Dishes/` folder
2. Create → Expo → Dish Data
3. Configure the dish properties (name, station, timing, icon)
4. The dish will be automatically available in the game (both editor and builds)

## Working with Progression

**To edit progression:** Edit `Assets/Resources/Data/progression_config.json` directly

This JSON file controls:
- Level requirements (XP thresholds)
- Which dishes unlock at each level
- Reward descriptions

## No Build Steps Required

Everything in the Resources folder is automatically included in builds. No copying or syncing needed!

## Troubleshooting

If you see errors like:
- "No dishes found in Resources/Data/Dishes!"
- "Loaded 0 dishes from disk"

Create DishData assets in `Assets/Resources/Data/Dishes/` folder.
