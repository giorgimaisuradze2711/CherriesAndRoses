# Multiplayer conversion — required manual Editor steps

All C# script changes for the Netcode for GameObjects (NGO) + Relay conversion are done and
committed to the working tree. What's left is Editor-only work that can't be done safely as a
raw text edit (scene/prefab YAML is corruption-prone; UGS account linking has no file
equivalent at all). Go through these in order.

## 1. Let packages resolve

`Packages/manifest.json` now lists `com.unity.netcode.gameobjects`, `com.unity.transport`,
`com.unity.services.core`, `com.unity.services.authentication`, `com.unity.services.relay`.
Open the project and let Package Manager resolve them. If a pinned version fails to resolve,
remove that line and use **Package Manager > Add package by name** to install the latest
instead — if a newer version renamed an API, the compiler will point at the exact line; the
likely spot is `NetworkBootstrap.cs`'s `RelayServerData` constructor call and `using`s.

## 2. Link Unity Gaming Services

**Project Settings > Services** (or the Unity Cloud dashboard) — link this project to a Unity
Cloud project, then enable **Relay** and **Authentication** (the Lobby service is *not* used —
this build shares Relay join codes manually, no room listing/matchmaking). Anonymous
authentication is enough, no real user accounts needed.

## 3. Create the MainMenu scene

**File > New Scene** (Basic template) → save as `Assets/Scenes/MainMenu.unity`. Open
**File > Build Settings** and drag `MainMenu` to the top of the scene list (index 0), above
whatever your existing gameplay scene is (unchanged — not renamed by this conversion).

## 4. Build the MainMenu scene hierarchy

Create a `Bootstrap` GameObject holding:
- `NetworkManager` (NGO component) + `UnityTransport` component.
- `NetworkBootstrap.cs` — wire its `Network Manager` field to the `NetworkManager` above and
  its `Transport` field to the `UnityTransport` above.

Add a Canvas with three panels:
- **MainMenuPanel**: `MainMenuController.cs` — wire `Main Menu Panel` (itself),
  `Host Join Panel` (below), and a Play button + Quit button.
- **HostJoinPanel** (start inactive): `HostJoinMenuController.cs` — wire `Network Bootstrap`
  (the Bootstrap object), `Host Join Panel` (itself), `Lobby Panel` (below), a Host button, a
  Join button, a join-code `TMP_InputField`, and a status `TextMeshProUGUI` for
  errors/progress ("Starting host...", "Couldn't join with that code.", etc).
- **LobbyPanel** (start inactive): `LobbyController.cs` — wire `Network Bootstrap`, a join-code
  `TextMeshProUGUI` (host-only, shows the code to share), a player-count `TextMeshProUGUI`, a
  Start button (host-only — hidden automatically for joining clients), and set
  `Gameplay Scene Name` to your actual gameplay scene's name (defaults to `SampleScene`).

## 5. Gameplay scene setup

In your gameplay scene:
- Add `NetworkObject` to the `CycleManager`, `ScoreManager`, and `Holder` GameObjects (each is
  a single in-scene object — no need to spawn/instantiate them).
- Add a `CinemachineBrain` component to the Main Camera if it doesn't already have one (each
  client only needs to see their own player; no per-player camera rig work was added since
  the existing camera setup wasn't touched).

## 6. Player prefab (`Assets/Prefabs/Characters/Player/Girl.prefab`)

- **Move `InventoryManager` onto this prefab.** Today it's its own standalone GameObject named
  `InventoryManager` in the scene, wired via a serialized field to the one Player instance.
  `InventoryManager.cs` now resolves its Player via `GetComponent<Player>()` instead (so every
  spawned player gets their own inventory), which means the component has to physically live
  on the Girl prefab (as a sibling of `Player`/`PlayerAnimator`/`CharacterController`), not on a
  separate GameObject. Add the `InventoryManager` component to `Girl.prefab` directly, then
  delete the old standalone `InventoryManager` GameObject from the scene.
- Add a `NetworkObject` component.
- Add a `NetworkTransform` component, set to **Owner Authoritative**.
- Add a `NetworkAnimator` component (wire its `Animator` field to the same Animator
  `PlayerAnimator.cs` already uses), also **Owner Authoritative** — this replicates
  `PlayerAnimator`'s locally-driven animation state to every other client.
- In `NetworkManager`'s inspector (on the Bootstrap object in MainMenu), set **Player Prefab**
  to this prefab, and add it to the **Network Prefabs** list.

## 7. AI prefabs (`Dog.prefab`, `MommyBunny.prefab`)

- Add `NetworkObject` + `NetworkTransform` (Server Authoritative, the default) to each — their
  `Chaser` logic now only runs server-side.

## 8. Collectible prefabs

Every prefab under `Assets/Prefabs/Collectibles/**` carrying `Collectible.cs` or `Cyclable.cs`
(~20 fruit/flower prefabs) needs a `NetworkObject` component added at the prefab level, so
`RequestPickUpServerRpc` and the picked-state sync work. Since they're already placed as
instances in the gameplay scene, saving the prefab change should propagate automatically —
just double check a few in the scene still show the `NetworkObject` component afterward (an
instance previously "unpacked" from its prefab would need the component added manually).

## 9. Testing

- Use **Window > Multiplayer Play Mode** to run 2+ simulated clients in one Editor session for
  quick iteration before testing over a real Relay connection across two machines.
- End-to-end check: from `MainMenu`, host creates a game and notes the join code; a second
  client joins by pasting the code in the Lobby panel; host presses Start and both land in the
  gameplay scene; both move around and see each other; each picks up items independently
  (confirm the same item can't be collected twice); each deposits at the Holder — score updates
  for both; the Dog/MommyBunny chasers correctly target whichever player triggered them
  (including when it's *not* the host who picked up the trigger item — this was the trickiest
  part of the conversion to get right).

## Known follow-ups (not required, but worth knowing about)

- Player spawn points aren't customized — NGO will spawn all players at the default
  transform/origin unless you add a spawn-point strategy later.
- The `BabyBunny` crying reaction (`BabyBunnyAnimator.cs`) only plays on the screen of
  whichever player picked up the balloon flower, not on other connected clients — its
  static-event trigger only fires client-side today. Low priority (purely cosmetic), but if it
  matters later, move that notification server-side the same way `Player.NotifyPickedUp` was
  done for the Dog/MommyBunny chase trigger.
- `NetworkBootstrap.cs`'s exact Relay SDK calls (`RelayServerData` constructor, `Allocation`/
  `JoinAllocation` shapes) were written against the current documented API shape but should be
  double-checked once the exact package versions resolve (see step 1).
