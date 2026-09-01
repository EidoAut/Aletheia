# Desktop Theme

Aletheia Desktop uses a dark-first quantitative-research design system rather than native WinForms chrome.

## Palette

The centralized palette lives in `ThemePalette`:

- near-black application background;
- a distinct graphite sidebar and header;
- layered neutral surfaces for cards, inputs and plots;
- subtle and emphasized borders;
- near-white primary text plus two secondary text levels;
- cyan as the primary analytical accent;
- amber/gold as the EIDO/Aletheia warning, selection and high-emphasis accent;
- restrained positive, negative and warning colors.

Colors are not scattered through page code. Semantic accents communicate meaning, while amber is reserved primarily for product identity, selection and high-emphasis actions.

## Reusable surfaces

The visual primitives are implemented in the desktop project without a third-party UI framework:

- `SurfacePanel` provides rounded bordered cards;
- `AletheiaButton` provides primary, secondary, ghost and danger actions;
- `NavigationButton` provides grouped sidebar destinations;
- `KpiControl` and `MetricStripControl` provide adaptive metric cards;
- `DataGridCardControl` provides titled diagnostic tables;
- `ActivityBarControl` and `StatusDotControl` provide application state;
- `AletheiaChartControl` provides a common card and empty-state frame for ScottPlot.

`DrawingUtilities` owns rounded geometry and color blending. `ControlStyler` remains the fallback for native inputs and tabular controls.

## Product identity

`BrandMarkControl` renders the Aletheia product name and EIDO Automation identity directly in the sidebar. The vector mark is deliberately self-contained so the desktop does not depend on a loose image file at runtime.

## Layout

The shell uses:

- a 232-pixel grouped sidebar;
- a contextual header with page, dataset and action areas;
- a padded analytical content area;
- a compact status bar;
- card-based page layouts with consistent seven-pixel gutters.

The design target is a professional research workstation: dense enough for technical comparison, but with explicit visual hierarchy and separation between controls, evidence and methodological warnings.

## DPI and accessibility

The desktop project enables WinForms and `PerMonitorV2` high-DPI mode. Controls use docking and layout containers instead of full-form absolute positioning. Owner-drawn controls expose focus cues, keyboard activation and accessible names.
