# Level Data Storage Using Colors (RGBA)

This document describes a level storage format based on images, where **each pixel represents a single map cell**, and its RGBA color encodes the properties of that cell.

---

## Map Size

- The map size is defined by the **image resolution in pixels**.
- Both width and height **must be greater than 0**.
- Each pixel corresponds to exactly one tile on the map.

---

## Design Goals

- **Tile ID is defined by color**, making levels easy and intuitive to create.
- Additional data is stored in a way that does not affect visual readability.
- The format must be:
  - unambiguous,
  - easy to parse,
  - editable using standard image editors.

---

## Channel Usage

### RGB Channel – Tile ID (Visible Color)

The **RGB channels** are used **only to define the tile `id`**.

- Each `id` (0–7) is mapped to a **fixed, clearly distinguishable color**.
- Level designers work directly with colors, without needing to think about bit values.

Example color mapping:

| ID | Meaning        | RGB Color |
|----|----------------|-----------|
| 0  | Default Tile   | (0, 0, 0) |
| 1  | Path Straight  | (255, 255, 0) |
| 2  | Path Turn      | (0, 255, 255) |
| 3  | Factory        | (0, 255, 0) |
| 4  | Boiler         | (0, 0, 255) |
| 5  | Splitter       | (255, 0, 0) |
| 6  | Merger         | (255, 0, 255) |
| 7  | Obstacle       | (255, 255, 255) |

> ⚠️ Colors must be **exact** (no anti-aliasing or lossy compression).

---

### Alpha Channel – Rotation and Power

The **Alpha (A) channel** stores additional numerical data.

#### Bit Layout (8 bits)

```bash
PPPPPP RR
```

| Bits | Description |
|----|------------|
| `RR` (2 bits) | `rotation` (0–3) |
| `PPPPPP` (6 bits) | `power` (0–63) |

- `power` uses inverted logic for better defaults:
  - `power = 0` is stored as `111111`
  - `power = 63` is stored as `000000`
- Alpha value `255` still represents full opacity, ensuring visual consistency in image editors.

#### Encoding Rules

```bash
storedPower = 63 - power
alpha = (storedPower << 2) | rotation
```

#### Example

```bash
power = 0
rotation = 1

storedPower = 63 -> 111111
rotation = 1 -> 01

Alpha bits: 11111101
Alpha value: 253
```

---

## Data Decoding

1. Read pixel color `(R, G, B, A)`
2. Determine `id` from `(R, G, B)`
3. Extract from Alpha:

```bash
rotation = A & 0b00000011
power = 63 - ((A >> 2) & 0b00111111)
```

---

## Notes

- Use **lossless formats** only: PNG, BMP, QOI
- Disable:
- anti-aliasing,
- color correction,
- lossy compression.
- Providing a **color palette or template image** for designers is strongly recommended.

---