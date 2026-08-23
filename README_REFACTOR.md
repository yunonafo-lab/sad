# Plugin Refactor: Modular Structure

## Overview
The original 15,000+ line `Plugin.cs` has been split into organized partial classes for better maintainability and development.

## File Structure

### Core Files
- **Plugin.Core.cs** - Initialization, Awake, and core setup
- **Plugin.Configuration.cs** - Config binding and settings management

### Feature Modules (To be created)
- **Plugin.UI.cs** - Menu rendering and UI styles
- **Plugin.ESP.cs** - Player ESP rendering and overlays
- **Plugin.Genes.cs** - Character customization and gene management
- **Plugin.Teleport.cs** - Teleportation and waypoint system
- **Plugin.Spawner.cs** - Prefab spawning functionality
- **Plugin.HostTools.cs** - Kick, ban, and room management
- **Plugin.ServerBrowser.cs** - Server listing and joining
- **Plugin.Spectate.cs** - Spectator mode and camera control
- **Plugin.Network.cs** - Photon callbacks and network events
- **Plugin.Utilities.cs** - Helper methods and type resolution

## How to Use

Each partial class file:
1. Declares `public partial class Plugin`
2. Contains only related fields and methods
3. Uses clear region markers for organization
4. Maintains full access to shared Plugin state

Example method from `Plugin.Genes.cs`:
```csharp
public partial class Plugin
{
    // Gene-related fields
    private float[] geneCurrent = new float[14];
    
    // Gene-related methods
    private void UpdateGenes()
    {
        // Implementation
    }
}
```

## Compilation

The modular structure compiles as a single `Plugin` class due to C# partial classes:
```
Plugin.cs + Plugin.Core.cs + Plugin.Configuration.cs + ... = Plugin (one class)
```

## Benefits

✅ **Better Organization** - Related functionality grouped together  
✅ **Easier Navigation** - Smaller files are easier to search and read  
✅ **Parallel Development** - Multiple developers can work on different features  
✅ **Reduced Merge Conflicts** - Changes to different features don't conflict  
✅ **Maintainability** - Clear separation of concerns  
✅ **Testing** - Easier to unit test individual features  

## Next Steps

1. Extract remaining code from original Plugin.cs
2. Organize into appropriate partial class files
3. Remove Plugin.cs when all code is migrated
4. Update project documentation

## Notes

- All private fields remain private and isolated to their partial class
- Shared state is accessible across all partials (same class)
- No breaking changes to compiled output
- IntelliSense shows all methods from all partial classes
