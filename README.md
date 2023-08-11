# Distant Seas

Distant Seas is a [Dalamud](https://github.com/goatcorp/Dalamud) plugin that aims to improve Ocean Fishing:

- Automatic, context-aware bait suggestor
- Recorder of events of each boat you're on
- Achievement & point tracker
- A global point leaderboard

## Updating spreadsheet information

Distant Seas uses the [Ocean Fishing Data](https://docs.google.com/spreadsheets/d/1R0Nt8Ye7EAQtU8CXF1XRRj67iaFpUk1BXeDgt6abxsQ/edit#gid=1841418685) spreadsheet (from the [Fisherman's Horizon](https://discord.gg/fishcord) Discord server). This data is parsed by the DistantSeas.SpreadsheetSpaghetti project into JSON files used by the plugin.

The sheets are parsed with tab-separated values, which do not contain color or image information. To fix this, a custom copy of the sheet is maintained by the creator (Tyo'to Tayuun) replacing them with text equivalents. Thank you!

Note that the SpreadsheetSpaghetti project is *incredibly* unoptimized - using up to 400MB of RAM and allocating several gigabytes of memory in the small object heap. I wrote it in a rush to get the parsing code working, and plan to revisit and clean it up eventually.

To run it, provide the path to your game path, and the output directory:

```shell
dotnet run --project DistantSeas.SpreadsheetSpaghetti -- "G:/Steam/steamapps/com
mon/FINAL FANTASY XIV Online/game/sqpack" ./Data
```
