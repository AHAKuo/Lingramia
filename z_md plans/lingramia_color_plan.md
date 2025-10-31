# 🎨 Lingramia Re-Coloring Plan
### Theme: Playful Tech (Subtle Red-Purple Accent)

This plan defines the new visual identity for Lingramia, focusing on improved readability, stronger hierarchy, and a cohesive dark-mode aesthetic infused with subtle red-purple hues.

---

## 🧭 1. Core Palette

| Role | Color | Usage |
|------|--------|--------|
| **Primary Background** | `#18171C` | Main app background |
| **Secondary Background** | `#222026` | Sidebars, panels, containers |
| **Surface / Hover** | `#2D2A33` | Active states, hovered items |
| **Accent Primary (Purple)** | `#A66CFF` | Buttons, active indicators, highlights |
| **Accent Secondary (Soft Red)** | `#E05C6E` | Warnings, key highlights, interactive hover |
| **Text Primary** | `#EAEAEA` | General text |
| **Text Secondary** | `#B6B3BD` | Subtext, labels |
| **Disabled Text** | `#6F6C76` | Disabled elements |
| **Border / Divider** | `#3B3842` | Panel dividers, outlines |
| **Success Green (optional)** | `#7DE27D` | Confirm or success messages |
| **Warning Orange (optional)** | `#E3A86F` | Caution or pending states |

---

## 🧱 2. Layout Depth & Structure

- Apply **soft shadows** for panels:
  ```
  shadow: 0 2px 4px rgba(0,0,0,0.4)
  ```
- Replace hard borders with subtle transparency:
  ```
  border: 1px solid rgba(255,255,255,0.05)
  ```
- Panels should visually layer like cards rather than flat sections.

---

## 💡 3. Input Fields & Buttons

### Fields
| Element | Color |
|----------|--------|
| Background | `#2D2A33` |
| Text | `#EAEAEA` |
| Placeholder | `#8A8692` |
| Border (default) | `#3B3842` |
| Border (focused) | `#A66CFF` |

### Buttons
| State | Background | Text | Notes |
|--------|-------------|------|-------|
| Default | `#3A3644` | `#EAEAEA` | subtle purple undertone |
| Hover | `#A66CFF` | `#FFFFFF` | elevate via brightness |
| Pressed | `#8E5CDD` | `#FFFFFF` | |
| Disabled | `#29262E` | `#6F6C76` | |

---

## 🗉 4. Sidebar (Locbooks Panel)

| Element | Color / Effect |
|----------|----------------|
| Background | `#222026` |
| Active Locbook | `#3A3644` |
| Hover State | `#2E2A34` |
| Text Active | `#FFFFFF` |
| Text Inactive | `#B6B3BD` |
| Divider Line | `#3B3842` |
| Selected Indicator | 2px border-left in `#A66CFF` |

---

## 🧭 5. Top Bar & Menus

| Element | Color / Effect |
|----------|----------------|
| Top Bar Background | `#1A191E` |
| Menu Text | `#D0CDD5` |
| Menu Hover | `#FFFFFF` |
| Search Box | `#2D2A33` background, accent border on focus |
| Accent Strip | 2 px line below bar in gradient `linear-gradient(90deg, #A66CFF, #E05C6E)` |

---

## 🗾 6. Page Editor Area

| Element | Color / Effect |
|----------|----------------|
| Page Background | `#18171C` |
| Panel Background | `#222026` |
| Selected Page Highlight | `#3B3643` |
| Empty Page State Icon | `#6F6C76` |
| Empty Page Text | `#A39FAB` |
| Actionable Link | `#A66CFF` hover → `#E05C6E` |

---

## 🌈 7. Typography

| Type | Font | Size | Color |
|------|------|------|-------|
| Header | Inter SemiBold | 16–18 px | `#EAEAEA` |
| Body | Inter Regular | 14 px | `#B6B3BD` |
| Label | Inter Medium | 12 px | `#8A8692` |

---

## 🪄 8. Thematic Touches

- Use **slight color gradient** in accent UI (buttons, highlights):  
  `linear-gradient(90deg, #A66CFF, #E05C6E)`
- Tooltip background: `#2D2A33` with 1 px `#A66CFF` outline.
- Focus ring: `#E05C6E` glow (`0 0 6px rgba(224,92,110,0.6)`).

---

## 🧰 9. Implementation Notes

- Centralize all color values in a **Resource Dictionary** (Avalonia or XAML):
  ```xaml
  <Color x:Key="AccentPrimary">#A66CFF</Color>
  <Color x:Key="AccentSecondary">#E05C6E</Color>
  <Color x:Key="PanelBackground">#222026</Color>
  <Color x:Key="WindowBackground">#18171C</Color>
  ...
  ```
- Use **DynamicResource** bindings so you can later add light/dark theme switching.
- Gradually phase in the palette: start with main panels → fields → top bar → typography.

---

## 🧭 10. Visual Preview Direction (Moodboard)

- **Base Feel:** VS Code × Figma × Obsidian hybrid  
- **Accent Glow:** Electric purple shifting to warm red  
- **Lighting:** Dim ambient with neon highlights  
- **Emotion:** Sleek, creative, confident

---

> 💬 *Lingramia’s UI should feel like a modern creative workspace — dark, warm, and energetic — while keeping clarity and precision for localization work.*

