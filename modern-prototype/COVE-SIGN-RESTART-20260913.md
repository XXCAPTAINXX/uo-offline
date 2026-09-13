# Cove sign start fix — September 13, 2026

The moved cove board could open its menu nine tiles from the clearing, but StartError recognized only the older expedition markers and an eight-tile camp radius. Added the actual HavenCoveBoard as a valid nearby start point, bound to its exact camp, same facet, within three tiles, eight elevation units and line of sight. Other start requirements unchanged.

Isolated test recreated board (4211,2930,0), player (4211,2931,0), clearing (4214,2922,0): challenge starts with 15 enemies; remote board access rejected. Clean live save/shutdown and fresh Saves backup with hashes and old DLL/source under servuo-before-cove-sign-*. Live build zero warnings/errors, startup and login/relay probe passed. No placement or client asset changes.
