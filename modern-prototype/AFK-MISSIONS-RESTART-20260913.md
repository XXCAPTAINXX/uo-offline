# AFK companion missions — September 13, 2026

Added [afk toggle/on/off, manual AFK button on offline mission settings, and a persisted automatic-AFK toggle (default on) after five minutes without player input. Existing offline plans must be enabled; starting AFK via the plan button also enables that plan. Manual AFK ends with movement or explicit toggle/off. Automatic AFK ends on monitored player input. Login resets transient AFK state. Keepalive packets and outgoing mission timer updates are not activity.

Uses the same offline dispatch validation, route rotation, durations, reward storage and completion path. Returning honors the existing finish-trip/recall-early option. Requirements, combat restrictions and reward capacity checks retained. No backdated trips while server stopped. Plan serialization v1 reads existing v0 saves with automatic AFK enabled.

Input wrappers preserve native packet handlers and throttle callbacks for movement, speech, attack/use/lift/drop, action/equip, targeting, text-entry and gump response. No authentication packets intercepted.

Connected isolated test passed manual AFK and simulated inactivity dispatch; repeated pulse preserves due time; finish/recall on return; activity resets auto AFK; disabled plan and auto switch; five-minute boundary; settings construction. Live build zero warnings/errors. Clean save/shutdown and fresh backup with Saves hashes and prior DLL/sources under servuo-before-afk-missions-*. Startup and login/server-list/relay probe passed. No client asset changes.
