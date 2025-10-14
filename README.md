### Running

To launch the server partition, run:
```sh
dotnet run --server
```

To launch the client partition, run:
```sh
dotnet run --client
```

There can be multiple clients on one machine. Clients bind to a random non-restricted port.

### Implemented & working features
- [x] Movement (through MoveCommand)
- [x] Weapons (through UseWeaponCommand)
- [x] Rendering of room and exits
- [x] Rendering of players
- [x] Rendering of skeletons
- [ ] Rendering of loot drops
- [ ] Interaction with loop drops (Can be implemented as nested logic within MoveCommand)
- [x] Basic Skeleton/AI behavior
- [ ] Skeleton/AI combat (needs UseWeaponCommand to be returned from TickAI implementations) 
- [x] Basic room generation (basic logic in WorldGrid::GenRoom)
- [ ] Difficulty-based room generation (Needs a Singleton RNG, RoomFactory and AbstractEnemyFactory)
- [ ] Room loot and enemies generation (Needs a RoomFactory, related to difficulty-based gen)