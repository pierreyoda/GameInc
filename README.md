# Game Inc.

A Game development studio management game made with Unity and C#7, inspired by the likes of Game Dev Story and Game Dev Tycoon.

Implemented Features (very WIP) :
- Database System loading custom JSON files (supporting comments and multi-line strings) into game objects at runtime. 
The entire game is intended to be described by these files : gaming platforms, random name generation, employee skill sets, game features, events, world news...
The base game is in the Assets/Core module.
- Script System : statically typed embedded toy language heavily inspired by Rust for its syntax and some of its concepts, with a touch of Typescript and Python. The language is constantly evolving but the abstraction to interact with it from C# code should not change too much. The parsing is currently done with the [Shunting-Yard algorithm] to build an AST that can be recursively evaluated (no bytecode VM yet).
Currently supports primitives (boolean, integer, float, string, Database ID, Date), variables, function calls, arrays. All type errors are caught on parsing.
Implemented in less than 2000 lines of modern C# in the Assets/Script module.
- Staff Simulation : random employee generation with skills and salary derived from the hiring method
- User Interface : build new rooms, start new games, pause or speed up the simulation.
- Market Simulation : dynamic sales of released products (Game or Game Engines only for now) with scoring and hype.
Implemented so as to enable regional-specific market tendencies in the future.

[Shunting-Yard algorithm]: https://www.wikiwand.com/en/Shunting-yard_algorithm
