# Wagenheimer Package Hub — Agent Notes

UPM package (repo root = package root), installed via git URL.

## UI Toolkit: leading-icon text (do not regress)

Never put an emoji/symbol inline at the start of a `Button.text` (or a lone `Label`). On Windows the
fallback emoji glyph draws wider than Unity measures it, so the following text runs over the icon
(e.g. "heck Updates" on top of the update glyph). Instead route buttons through
`PackageHubUIStyle.CreateButton` (which calls `PackageHubUIStyle.ApplyIconText`) or call
`PackageHubUIStyle.ApplyIconText(button, text)` directly; for title labels use
`PackageHubUIStyle.CreateIconLabel(text)`. This splits the leading icon into its own `min-width`
element so the two never overlap. This is the only supported way to show an icon before a label.
