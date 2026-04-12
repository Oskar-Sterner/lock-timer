# LockTimer

Minimalist speedrun-timer plugin for Deadlock (Deadworks managed).

## Commands

| Command | Effect |
|---|---|
| `!start1` / `!start2` | Capture corner 1 / 2 of the start zone at crosshair hit |
| `!end1` / `!end2` | Capture corner 1 / 2 of the end zone |
| `!savezones` | Persist both zones for the current map and render edges |
| `!delzones` | Remove zones for the current map |
| `!zones` | Show which pending corners are staged |
| `!pb` | Show your PB on the current map |
| `!top` | Show top-10 times on the current map |
| `!reset` | Reset your own run state |

Timer starts when your feet leave the start zone and stops when they enter the end zone. Re-entering start while running resets you to InStart.

## Database

SQLite file at `…/managed/plugins/LockTimer/locktimer.db`. PB records only (one row per `steam_id, map`).

## Manual smoke checklist

After building and loading:

- [ ] 40 marker particles (20 per zone: 8 corners + 12 edge midpoints) spawn on `!savezones`
- [ ] Walking from start to end records a time in chat
- [ ] Beating your PB shows `(new PB! prev …)` message
- [ ] Slower run shows `(pb …)` message, no DB change
- [ ] `!delzones` removes particles and clears the DB rows
- [ ] Disconnect mid-run, reconnect — no stale state
- [ ] Map change mid-run abandons the run cleanly
