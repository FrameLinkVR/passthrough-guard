# Vendored fonts (release/rig step)

The settings UI uses the FrameLink brand type **self-hosted** — no CDN, so the loopback desktop
app never depends on the network for type. The `.woff2` files are **not committed** (they are
@fontsource redistributables pulled at release time). Until they are present, the system-font
fallback in each `var(--sans)` / `var(--mono)` stack renders cleanly.

Drop these three subset files here (filenames must match `design-system.css`'s `@font-face` rules):

| File | Source (@fontsource, MIT/OFL) |
|---|---|
| `space-grotesk-latin-500-normal.woff2` | `@fontsource/space-grotesk` → `files/space-grotesk-latin-500-normal.woff2` |
| `space-grotesk-latin-700-normal.woff2` | `@fontsource/space-grotesk` → `files/space-grotesk-latin-700-normal.woff2` |
| `jetbrains-mono-latin-500-normal.woff2` | `@fontsource/jetbrains-mono` → `files/jetbrains-mono-latin-500-normal.woff2` |

How to fetch (on a networked machine):

```sh
npm pack @fontsource/space-grotesk @fontsource/jetbrains-mono
# extract the latin-*-normal.woff2 subsets from each tarball's files/ dir into this folder
```

The installer ships whatever is in this folder beside the exe under `web/fonts/`.
The fonts are Space Grotesk (OFL) and JetBrains Mono (OFL) — include their licenses in the release.
