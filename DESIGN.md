---
name: CommandSynergy
description: Archival Commander deckbuilding workspace with tactile analysis cues.
colors:
  ember-rust: "#8B3F29"
  ember-rust-bright: "#A7623B"
  moss-green: "#1F5B4B"
  gilt-brass: "#8D6A2F"
  linen-surface: "#FBF7EF"
  vellum-surface: "#FFFCF6"
  parchment-sand: "#EFE6D6"
  mist-sage: "#E6EBDF"
  ink: "#14261F"
  title-ink: "#19352C"
  muted-olive: "#5D685F"
  cream-text: "#FFFAF1"
typography:
  display:
    fontFamily: "Literata, Georgia, serif"
    fontSize: "clamp(1.8rem, 3vw, 2.8rem)"
    fontWeight: 700
    lineHeight: 1.05
    letterSpacing: "-0.03em"
  headline:
    fontFamily: "Literata, Georgia, serif"
    fontSize: "clamp(1.55rem, 2.4vw, 2.2rem)"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: "-0.03em"
  title:
    fontFamily: "Literata, Georgia, serif"
    fontSize: "clamp(1.25rem, 1.8vw, 1.65rem)"
    fontWeight: 600
    lineHeight: 1.15
    letterSpacing: "-0.03em"
  body:
    fontFamily: "Familjen Grotesk, Segoe UI, sans-serif"
    fontSize: "0.96rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Familjen Grotesk, Segoe UI, sans-serif"
    fontSize: "0.76rem"
    fontWeight: 700
    lineHeight: 1
    letterSpacing: "0.18em"
rounded:
  sm: "12px"
  md: "16px"
  lg: "22px"
  xl: "30px"
  pill: "999px"
spacing:
  xs: "0.35rem"
  sm: "0.5rem"
  md: "0.75rem"
  lg: "1rem"
  xl: "1.35rem"
  xxl: "1.5rem"
components:
  button-primary:
    backgroundColor: "{colors.ember-rust}"
    textColor: "{colors.cream-text}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "0.74rem 1.1rem"
  button-primary-hover:
    backgroundColor: "{colors.ember-rust-bright}"
    textColor: "{colors.cream-text}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "0.74rem 1.1rem"
  button-secondary:
    backgroundColor: "{colors.vellum-surface}"
    textColor: "{colors.title-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "0.68rem 0.96rem"
  input-search:
    backgroundColor: "{colors.vellum-surface}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.pill}"
    padding: "0.98rem 1.1rem"
  panel-workspace:
    backgroundColor: "{colors.linen-surface}"
    textColor: "{colors.title-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.xl}"
    padding: "1.35rem"
  chip-theme:
    backgroundColor: "{colors.mist-sage}"
    textColor: "{colors.title-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "0.45rem 0.75rem"
  nav-appbar:
    backgroundColor: "{colors.vellum-surface}"
    textColor: "{colors.title-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.xl}"
    padding: "0.7rem 1rem"
---

# Design System: CommandSynergy

## 1. Overview

**Creative North Star: "The Commander Workbench"**

CommandSynergy should feel like a deckbuilder's table that has already been arranged by someone with taste. The surface is bold, tactile, and strategically sharp: parchment light instead of sterile white, structured like an instrument instead of a dashboard, and warm enough to feel collectible without ever slipping into fantasy cosplay.

The interface earns confidence through disciplined signal placement. Large radii, soft ambient layers, and archival neutrals keep the workspace approachable; rust, moss, and gilt appear only where the system needs to direct action, confirm state, or spotlight meaning. This is not a productivity template with card art dropped into it. It is a purpose-built analysis surface for Commander players.

What the system rejects is explicit. It must not drift into generic SaaS or admin dashboard patterns, glossy futuristic styling, neutral dashboard chrome, filler surfaces, or generic productivity-tool cues. If a screen feels like clerical data entry instead of deliberate deck construction, it is wrong.

**Key Characteristics:**
- Light-first, archival surfaces with visible material warmth.
- Literary serif headlines over a sturdy grotesk body for collected authority.
- Soft layering and blur used sparingly to separate work areas, not to decorate them.
- Accent colors reserved for signals, actions, and analysis states.
- Responsive behavior that collapses structure cleanly at 1180px, 920px, and 720px without changing the system's voice.

## 2. Colors

The palette reads like tabletop materials under daylight: ember for commitment, moss for stability, gilt for analysis highlights, and linen neutrals that keep the interface tactile instead of clinical.

### Primary
- **Ember Rust** (#8B3F29): The commitment color. It belongs on primary actions, recovery actions, destructive moments, and the few places where the interface asks the user to act decisively.

### Secondary
- **Moss Green** (#1F5B4B): The system's stabilizer. Use it for status chips, focus outlines, structural accents, and positive confirmation where the UI needs calm authority instead of heat.

### Tertiary
- **Gilt Brass** (#8D6A2F): The analytic highlight. Use it for recommendation metadata, active library signals, and other moments that benefit from a collector's warmth rather than a warning tone.

### Neutral
- **Linen Surface** (#FBF7EF): The default panel ground. It should carry most primary work surfaces.
- **Vellum Surface** (#FFFCF6): The brightest inset surface. Use it inside buttons, drawers, and fields where an element needs to separate cleanly from the panel below it.
- **Parchment Sand** (#EFE6D6): The warm edge of the page background. It introduces tactile atmosphere without reading as beige sludge.
- **Mist Sage** (#E6EBDF): The cool counterweight in the page background and soft chips. It prevents the system from becoming all rust and cream.
- **Ink** (#14261F): The deepest body text color. Use it for dense reading, inputs, and anything that needs maximum legibility.
- **Title Ink** (#19352C): The headline and high-priority label color. Slightly richer than body ink so headings feel deliberate, not merely larger.
- **Muted Olive** (#5D685F): The secondary text and caption color. Use it for timestamps, sublabels, helper copy, and explanatory text that should recede without disappearing.
- **Cream Text** (#FFFAF1): Reserved for text on ember surfaces. Never use it as a free-floating neutral.

**The Signal Reserve Rule.** Ember Rust is scarce on purpose. If it starts appearing as decoration, the interface loses authority.

**The Counterweight Rule.** Moss Green and Gilt Brass support the system; they never compete with Ember Rust for primary attention on the same interaction.

## 3. Typography

**Display Font:** Literata (with Georgia fallback)
**Body Font:** Familjen Grotesk (with Segoe UI fallback)
**Label/Mono Font:** Familjen Grotesk for labels; monospace is restricted to diagnostic details only.

**Character:** The pairing should feel like annotated deck notes written by someone exacting. The serif carries judgment and craft. The sans carries speed, scannability, and interface discipline.

### Hierarchy
- **Display** (700, clamp(1.8rem, 3vw, 2.8rem), 1.05): Reserved for page-level titles and error-state headlines that need to feel authored, not templated.
- **Headline** (600, clamp(1.55rem, 2.4vw, 2.2rem), 1.1): Section anchors for workspace panels, pile headers, banners, and analysis groups.
- **Title** (600, clamp(1.25rem, 1.8vw, 1.65rem), 1.15): Subsection headings such as briefing blocks and interior panel titles.
- **Body** (400, 0.96rem, 1.5): The default reading rhythm for helper copy, explanatory states, and card-adjacent metadata. Long prose should stay within 65ch to 75ch.
- **Label** (700, 0.76rem, 0.18em letter-spacing, uppercase): Eyebrows, badges, metadata rails, and small operational labels. It should read like controlled instrumentation, never like marketing copy.

**The Instrument Label Rule.** Small labels are always uppercase, tightly tracked, and semantically useful. If a label is decorative, remove it.

**The Serif Threshold Rule.** Serif typography marks structure and importance. Do not use it for buttons, filters, data tables, or dense utility copy.

## 4. Elevation

This system is mostly flat with selective lift. Depth is conveyed first through tonal layering and warm-vellum surface changes, then reinforced with soft ambient shadows and restrained blur on drawers, overlays, and active emphasis states. Nothing should look like a floating glass card showroom.

### Shadow Vocabulary
- **Ambient Low** (`box-shadow: 0 10px 24px rgba(19, 30, 24, 0.07)`): Default panel and container lift. Use it to separate work surfaces from the page background without making them hover.
- **Ambient High** (`box-shadow: 0 22px 54px rgba(19, 30, 24, 0.10)`): App bar and hero workbench lift. This is for the largest structural surfaces only.
- **Overlay Lift** (`box-shadow: 0 28px 56px rgba(22, 30, 24, 0.22)`): Utility drawers and modal-grade surfaces that must clearly sit above the workspace.
- **Action Lift** (`box-shadow: 0 12px 24px rgba(139, 63, 41, 0.18)`): Primary buttons and rust-accent actions. The shadow should feel like pressure and readiness, not glow.

**The Lift On Intent Rule.** Elevation appears when the interface asks for attention or action. Flat is the resting state.

**The Blur Has A Job Rule.** Backdrop blur is permitted only on overlays and drawers where it clarifies layering. It is forbidden as decorative frosting on ordinary panels.

## 5. Components

Every component should feel tactile and confident. Rounded forms, warm neutrals, and restrained movement are the defaults. The system does not reward novelty for its own sake.

### Buttons
- **Shape:** Fully rounded capsules (999px) for primary and secondary actions, with enough height to feel substantial in hand.
- **Primary:** Ember Rust fill (#8B3F29) with cream text (#FFFAF1), label-weight typography, and padding around 0.74rem by 1.1rem. This is the action vocabulary for search, import, export, and decisive deck operations.
- **Hover / Focus:** Hover lifts by 1px with a stronger shadow. Focus uses a visible moss-tinted outline (3px on most controls, 2px plus offset on app-bar chips).
- **Secondary / Ghost / Tertiary:** Secondary actions use vellum or white-tinted fills, moss borders, and title-ink text. Destructive secondary actions reuse Ember Rust as a soft-tint background rather than inventing a second red.

### Chips
- **Style:** Fully rounded chips with compact horizontal padding, label typography, and tonal backgrounds rather than heavy borders.
- **State:** Theme chips use moss-tinted backgrounds for ordinary tags; alignment chips shift to moss, brass, or ember families depending on theme confidence. Chips are signal markers, not interactive pills unless the behavior requires it.

### Cards / Containers
- **Corner Style:** Large rounded work surfaces (30px outer containers, 22px interior cards, 16px inset metrics).
- **Background:** Linen and vellum neutrals dominate. Recommendations and activity banners may add brass or moss-tinted gradients when the system needs to guide attention.
- **Shadow Strategy:** Ambient Low by default, Ambient High only on structural anchors, Overlay Lift only on drawer-grade surfaces.
- **Border:** Fine moss-or-ink tinted strokes around 8 percent to 20 percent opacity. Borders separate surfaces quietly; they never shout.
- **Internal Padding:** Standard panel padding sits around 1.35rem, with inset metrics and cards stepping down to 1rem or 0.8rem.

### Inputs / Fields
- **Style:** Rounded fields with vellum backgrounds, faint inset highlights, and moss-tinted outlines. Inputs should feel embedded into the workbench, not dropped on top of it.
- **Focus:** A 3px moss outline with offset provides the primary focus treatment. Border darkening alone is insufficient.
- **Error / Disabled:** Error states move into ember borders and soft ember backgrounds. Disabled controls reduce opacity and lose lift, but remain legible.

### Navigation
- **Style, typography, default/hover/active states, mobile treatment:** The app bar behaves like a rounded control rail rather than a flat header. It uses Ambient High elevation, vellum gradients, serif titling, uppercase metadata, and capsule actions. At 720px and below, the rail stacks vertically, actions stretch full-width, and the utility drawer becomes edge-to-edge rather than pretending to be a tiny modal.

### Signature Component
- **Suggestion Result Card:** Recommended cards combine collector-style metadata pills, card presentation, and a single decisive action. The card should feel curated, not algorithmically sprayed. Recommendation metadata sits in brass-tinted capsules and never competes with the add action.

## 6. Do's and Don'ts

### Do:
- **Do** keep primary work surfaces on Linen Surface (#FBF7EF) or Vellum Surface (#FFFCF6) with large radii (22px to 30px) and soft ambient lift.
- **Do** use Ember Rust (#8B3F29) for commitment moments only: primary actions, recovery actions, and destructive emphasis.
- **Do** preserve uppercase label typography at 0.76rem with 0.18em tracking for badges, metadata, and interface instrumentation.
- **Do** collapse layout structure cleanly at 1180px, 920px, and 720px; mobile should feel like the same workbench, not a separate minimal app.
- **Do** keep focus visible with moss-tinted outlines and offsets. Keyboard state must remain obvious on every actionable control.

### Don't:
- **Don't** let the interface drift into generic SaaS or admin dashboard patterns. That includes anonymous white cards, interchangeable KPI tiles, and templated app-shell chrome.
- **Don't** introduce glossy futuristic styling. No neon accents, no slick sci-fi gradients, no synthetic glow language.
- **Don't** lean on neutral dashboard chrome. If a surface becomes gray, square, and emotionally blank, it is outside the system.
- **Don't** add filler surfaces just to group content. Every panel, chip, or banner needs a job.
- **Don't** borrow generic productivity-tool cues such as flat toolbar rows, generic tab strips, or monochrome utility panels that make deckbuilding feel like clerical data entry.
- **Don't** use decorative blur, gradient text, colored side-stripe borders, or any other visual shorthand that reads as template work rather than a crafted Commander tool.