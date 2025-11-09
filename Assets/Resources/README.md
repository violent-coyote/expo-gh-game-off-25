# Resources Folder

This folder contains assets that need to be loaded at runtime in builds (especially WebGL).

## Why this folder exists

Unity requires assets loaded at runtime via `Resources.Load()` to be in a folder named "Resources". In the editor, we can use `AssetDatabase` to load assets from anywhere, but in builds (WebGL, standalone, etc.), we need the Resources folder.

## Contents

- **Data/Dishes/** - All DishData ScriptableObjects that need to be available at runtime
- **Data/progression_config.json** - The progression configuration file

## Keeping Resources in sync

**IMPORTANT:** When you add or modify dishes in `Assets/Data/Dishes/`, you must copy them to `Assets/Resources/Data/Dishes/` before building.

### Easy way to sync:
Use the menu: **Expo → Setup Dish Resources**

This will automatically:
1. Copy all dish assets from `Assets/Data/Dishes` to `Assets/Resources/Data/Dishes`
2. Copy `progression_config.json` to `Assets/Resources/Data/`

### Manual way:
1. Copy dish `.asset` files from `Assets/Data/Dishes/` to `Assets/Resources/Data/Dishes/`
2. Copy `Assets/Data/progression_config.json` to `Assets/Resources/Data/`

## Build checklist

Before making a WebGL build, always:
- [ ] Run "Expo → Setup Dish Resources" menu command
- [ ] Verify dishes are in `Assets/Resources/Data/Dishes/`
- [ ] Verify `progression_config.json` is in `Assets/Resources/Data/`

## Troubleshooting

If you see errors like:
- "No dishes found in Resources/Data/Dishes!"
- "Loaded 0 dishes from disk"
- "Created default progression config"

This means the Resources folder is missing content. Run "Expo → Setup Dish Resources" to fix it.
