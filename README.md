# GW2 TaskManager

![logo](src/Resources/logo.png)

> Daily & weekly task checklist for Guild Wars 2 veterans — with GW2 API auto-sync, event timers, and dopamine animations.

---

## Features

- **Daily & Weekly checklists** — 44 built-in objectives (world bosses, map chests, dailycrafting, Wizard Vault, fractals, strikes...)
- **GW2 API auto-sync** — automatically checks off completed world bosses, map chests, daily crafting and Wizard Vault rewards
- **Event timers** — live countdown column for timed events (Shatterer, Chak Gerent, Octovine, Dragonstorm, Ley-line Anomaly, Pinata), sorted by imminence
- **Toast notifications** — desktop alerts 5 minutes before each event
- **Per-character objectives** — assign any objective to a specific character from your account
- **Custom objectives** — add your own daily/weekly tasks
- **3 themes** — Warm ☀️ / Slate 🌙 / Auto 🔄 (Auto follows the GW2 Tyria day/night cycle)
- **FR 🇫🇷 / EN 🇬🇧** — full language switch, persisted between sessions
- **Waypoint copy** — one click to copy a waypoint code to clipboard

---

## Installation

1. Go to the [**Releases**](../../releases) page
2. Download `GW2TaskManager_beta.zip`
3. Extract the zip anywhere
4. Run `GW2TaskManager.exe`

**No installation required. No .NET runtime required.** Works on Windows 10/11 x64.

> **Note:** Windows may show a SmartScreen warning on first launch ("Unknown publisher").  
> Click **More info** → **Run anyway**. This is expected for unsigned apps.

---

## GW2 API Key Setup

An API key is optional but unlocks auto-sync.

1. Log in at [account.arena.net](https://account.arena.net)
2. Go to **Applications** → **New Key**
3. Enable permissions: `account`, `characters`, `progression`
4. Paste the key in the app's API bar

Your key is stored encrypted locally (Windows DPAPI). It never leaves your machine.

---

## Screenshots

*Coming soon.*

---

## Built-in objectives

| Category | Daily | Weekly |
|---|---|---|
| World Bosses | Shatterer, Chak Gerent, Octovine, Dragonstorm... | — |
| Map Chests | Silverwastes, Verdant Brink, Dragon's Stand... | — |
| Daily Crafting | Lump of Mithrillium, Glob of Elder Spirit Residue... | — |
| Wizard Vault | Daily reward | Weekly reward |
| Fractals | Daily T4 | — |
| Strikes | Weekly strikes | CM strikes |
| Raids | — | Full clear |
| WvW / PvP | Daily pip track | — |

All objectives can be individually enabled/disabled. Disabled objectives are hidden from the To Do view.

---

## Stack

- **WPF / .NET 8** — Windows desktop
- **CommunityToolkit.Mvvm** — MVVM pattern
- **GW2 official API** — [api.guildwars2.com/v2](https://api.guildwars2.com/v2)

---

## Credits

Made with ❤️ by **FTs.1843** and **Murran.3841**

All donations appreciated — find us in-game!

---

## License

Personal use. Not affiliated with ArenaNet or NCSoft.  
Guild Wars 2 assets and trademarks belong to ArenaNet.
