# Data Forge

Data Forge is a data persistence framework for Unity. It encrypts, serializes, and saves your data classes to disk, and provides editor windows to inspect and edit saved data at development time.

## Installation

Install with Unity Package Manager using the Git URL below:

```text
https://github.com/phamkhacquang/DataForge.git
```

Open the editor tool from:

```text
Data Forge > Data Editor
```

## Quick setup

### 1. Create a Config

A `Config` holds the encryption key and selects the serializer and security strategy. Create one class per project (or one per data class if you need different settings):

```csharp
public class MyConfig : DataForge.Config
{
    protected override byte[] Key => new byte[] { 11, 243, 5, 78, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2 };
}
```

> You can generate a config file with a random key from `Data Forge > Data Editor > Create`.

### 2. Create a data class

Inherit from `BaseData<T, TConf>`, decorate it with `[Serializable]`, and add your fields:

```csharp
using System;
using DataForge;
using UnityEngine;

[Serializable]
public class UserData : BaseData<UserData, MyConfig>
{
    public string username;
    public int gold;
    public long lastLoginTicks;
}
```

That is all. Data Forge picks up the class automatically after the next compilation.

## Runtime usage

### Read-only access via `Cache`

`BaseData<T, TConf>.Cache` returns the current in-memory snapshot. Use it for **display and decision logic only** — never mutate the object it returns.

```csharp
int currentGold = UserData.Cache.gold;
```

### Mutate and save

Before modifying persisted data, call `Load()` to get the authoritative on-disk version, mutate it, then call `Save()`. Both methods must be called on the **main thread**.

```csharp
// ✅ Correct pattern: read Cache for the check, then Load before writing
var ud = UserData.Cache;
if (ud.gold >= 1000)
{
    ud = UserData.Load();
    ud.gold -= 1000;
    ud.Save();
}
```

> **Why load before saving?** The in-memory `Cache` is vulnerable to memory manipulation attacks from tools like Cheat Engine. Calling `Load()` retrieves the encrypted, which overwrites any unauthorized modifications and restores the authoritative state before you apply legitimate changes.

## Security

### XOR cipher (default)

The default security uses a repeating XOR key derived from the device's unique identifier. It is lightweight and suitable for obfuscating casual inspection, but provides no cryptographic strength.

```csharp
// Default — XOR with device ID
public class MyConfig : DataForge.Config
{
    protected override byte[] Key => new byte[] { ... };
}
```

### AES-128 / 192 / 256

Override the `Security` property to use AES-CBC with a 16-, 24-, or 32-byte key:

```csharp
using DataForge;

public class MyConfig : Config
{
    protected override byte[] Key => new byte[] { /* 16, 24, or 32 bytes */ };
    public override IDataSecurity Security => new AES(Key);
}
```

AES generates a random IV per save, prepended to the ciphertext, so each save produces different bytes even for identical data.

## Serialization

### JSON (default)

Uses `UnityEngine.JsonUtility`. Supports any `[Serializable]` Unity type with no additional dependencies.

### MemoryPack

Add the `ENABLE_MEMORY_PACK` scripting define symbol and reference the [MemoryPack](https://github.com/Cysharp/MemoryPack) package, then override `Serializer` in your config:

```csharp
public class MyConfig : DataForge.Config
{
    protected override byte[] Key => new byte[] { ... };
#if ENABLE_MEMORY_PACK
    public override IDataSerializer Serializer => new MemoryPackSerializer();
#endif
}
```

## Editor tools

### Data Editor

Inspect and edit any registered data class directly in the editor.

```text
Data Forge > Data Editor
```

- Select a data type in the left panel.
- Modify fields in the right panel.
- Click **Save** or press **Ctrl+S** to write the changes to disk.

### Remote Data Editor

View and edit data on a connected device at runtime without a debugger.

```text
Data Forge > Remote Data Editor
```

1. Add `BaseDataStreamServer` as a component to a GameObject in your scene.
2. Run the build on device — the component prints the host address on screen.
3. Enter that address in the Remote Data Editor and click **Load**.

> `BaseDataStreamServer` opens an HTTP server on port **8282**. Restrict network access appropriately; the server has no authentication.

### PlayerPrefs Editor

Browse, edit, and delete PlayerPrefs entries on Windows.

```text
Data Forge > PlayerPrefs Editor
```

## Optional field attributes

### `EnumData<TEnum, TItem>`

Maps each value of an enum to an item. Useful for per-enum-value configuration such as reward tables or sprite sets:

```csharp
[Serializable]
public class ItemData : BaseData<ItemData, MyConfig>
{
    public EnumData<ItemType, int> dropRates;
}
```

Access by enum key:

```csharp
int rate = ItemData.Cache.dropRates[ItemType.Sword];
```

### `[DisplayAsDateTime]`

Renders a `long` ticks field as a human-readable `yyyy-MM-dd HH:mm:ss,fff` string in the inspector:

```csharp
using DataForge.CustomEditor;

[Serializable]
public class UserData : BaseData<UserData, MyConfig>
{
    [DisplayAsDateTime(DateTimeKind.Utc)]
    public long lastLoginTicks;
}
```

When `DateTimeKind.Utc` is used, a **Local** toggle lets you view the value in local time without changing the stored UTC ticks.
