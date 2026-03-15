# LEP File Format (`.lep`)

This document describes the **LEP project file format**, used to store map/project 
data in a compact, binary format for editors and runtime usage.

## File Structure Overview

LEP files are **binary files** designed for efficient storage and fast read/write 
operations. Each file consists of:

1. **Header**           - identifies the file and format version
2. **Metadata**         - file name, date, editor version
3. **Map dimensions**   - width and height of the tile grid
4. **Tile data**        - sparse list of tile entries

## Header
| Field | Type | Size | Description |
| ----- | ---- | ---- | ----------- |
| Magic | byte[4] | 4 | Always `'L' 'E' 'P' 0` – identifies LEP files |
| Format Version | uint32 | 4 | Current version = 3. Used for backward compatibility |

## Metadata
| Field | Type | Size | Description |
| ----- | ---- | ---- | ----------- |
| File Name | string | variable | UTF-8 encoded string. Length prefixed with int32 |
| Date  | Int64 | 8 | File creation date in UTC ticks (`DateTime.Ticks`) |
| Editor Version | string | variable | UTF-8 encoded string. Length prefixed with int32 |

> ℹ Notes:
> - Strings are stored as: `[int32 length][UTF-8 bytes]`
> - Maximum string length: **1 MB**

## Map Size

| Field | Type | Size | Description |
| ----- | ---- | ---- | ----------- |
| Width | ushort | 2 | Number of columns (max **16384**) |
| Height | ushort | 2 | Number of rows (max **16384**) |

> ℹ Notes:
> - In versions < 3, width and height were stored as `uint32` (4 bytes)
> - In version 3, they are `ushort` (2 bytes) to reduce file size

## Tiles Data
Tiles are stored as a sparse list of entries. Only tiles with non-default values need to be written.

Default tile color:
```text
R = 0; G = 0; B = 0; A = 255
```

Tiles are stored sequentially after:
```text
int32 tileCount
```

The interpretation of tile entries **depends on the format version.**

## Tile Encoding (Version 3)
Version 3 introduces **palette-based compression** using operation codes.

Each tile entry starts with:

| Field | Type | Size | Description |
| ----- | ---- | ---- | ----------- |
| OpCode | byte | 1 | Determines how tile color is encoded |

Two encoding modes are supported.

### OP_DATA (0)
Stores a **full color value.**

| Field | Type | Size | Description |
| ----- | ---- | ---- | ----------- |
| OpCode | byte | 1 | Value = 0 (OP_DATA) |
| X | ushort | 2 | Column position (0-based) |
| Y | ushort | 2 | Row position (0-based) |
| R | byte | 1 | Red component (0–255) |
| G | byte | 1 | Green component (0–255) |
| B | byte | 1 | Blue component (0–255) |
| A | byte | 1 | Alpha component (can store additional properties) |

Total size: **9 bytes**

After reading this tile:
1. The color is inserted into the **palette cache**
2. Future tiles with the same color may use **OP_INDEX**

### OP_INDEX (1)
References a previously stored color from the palette.

| Field | Type | Size | Description |
| ----- | ---- | ---- | ----------- |
| OpCode | byte | 1 | Value = 1 (OP_INDEX) |
| X | ushort | 2 | Column position (0-based) |
| Y | ushort | 2 | Row position (0-based) |
| Palette Index | byte | 1 | Index of color stored in palette |

Total size: **6 bytes**

The palette index refers to an entry in the **64-slot palette cache.**

### Palette Cache
Version 3 introduces a small **color palette cache.**

Properties:
- Fixed size: **64 entries**
- Initially filled with zeroed entries
- Updated when `OP_DATA` tiles are read

Hash function used to compute palette slot:
```cs
(r * 3 + g * 5 + b * 7 + a * 11) & 63
```

When `OP_DATA` is processed:
1. The color is hashed
2. The palette entry at that index is replaced

> ℹ Notes:
> - Palette entries may be overwritten when hash collisions occur.

## Tile Encoding (Version 2):
In versions **2**, tiles were stored **without compression.**

Each tile stored its **full color directly**, with no palette.

| Field | Type | Size | Description |
| ----- | ---- | ---- | ----------- |
| X | int32 | 4 | Column position (0-based) |
| Y | int32 | 4 | Row position (0-based) |
| R | byte | 1 | Red component (0–255) |
| G | byte | 1 | Green component (0–255) |
| B | byte | 1 | Blue component (0–255) |
| A | byte | 1 | Alpha component (can store additional properties) |

Total size: **12 bytes**

> ℹ Notes:
> - No operation code or palette was used.
> - Tiles were simply stored sequentially.

## Tile Encoding (Version 1):
Deprecated and no longer supported by the current reader.

## Reading LEP Files
Steps:
1. Open file as a **binary stream.**
2. Read the **header** (magic + version).
3. Parse **metadata**: file name, date, editor version.
4. Read **map dimensions** (width, height)
5. Read **tile count** and allocate array
6. Decode tiles depending on version.
7. Tiles not stored in the file should be initialized to **default color** (R=0, G=0, B=0, A=255)

### For Version 2
Each tile is read as:
```cs
int32 x
int32 y
byte r
byte g
byte b
byte a
```
Values of `x` and `y` are cast to `ushort`.

### For Version ≥ 3
Each tile begins with an **OpCode.**

If OpCode is:
```text
0 -> OP_DATA
1 -> OP_INDEX
```

The tile is decoded accordingly and the palette cache is updated when necessary.

## Writing LEP Files
When writing **version 3** files:
1. Write **magic bytes** (`'L' 'E' 'P' 0`) and **format version** (`3`)
2. Write **metadata** strings as length-prefixed UTF-8
3. Write **date** in UTC ticks
4. Write **map width and height** as `ushort`
5. Write **tile count**
6. Initialize palette[64]
7. For each tile:
	- compute palette hash
	- if palette contains same color then write `OP_INDEX`
	- otherwise write `OP_DATA` and update palette

> ℹ Notes:
> - Validate that width/height ≤ 16384 and tile array is non-null
> - Stored in binary for efficiency

## Backward Compatibility

| Version | Differences |
| ------- | ----------- |
| 1 | Deprecated format. Not supported. Better not use. |
| 2 | Original binary format. |
| 3 | `ushort` positions + palette compression |

Readers should:
1. detect the format version
2. select the correct tile decoding method

## Advantages of LEP Format
- **Compact:** small footprint due to binary storage and ushort for positions
- **Versioned:** backward-compatible reading of older versions
- **Editable:** metadata and tile color data allow easy inspection/modification

## Example Structure in Memory (C#)
```cs
ProjectData project = new ProjectData
{
    fileName = "MyLevel.lep",
    date = DateTime.UtcNow,
    editorVersion = "1.0.5",
    width = 128,
    height = 128,
    tiles = new TileData[width * height]
};

// Example tile
tiles[0] = new TileData
{
    x = 0,
    y = 0,
	color = new ColorData
	{
		r = 255,
		g = 0,
		b = 0,
		a = 255
	}
};
```

> ℹ Notes:
> - Each tile corresponds to a single cell in the map grid.