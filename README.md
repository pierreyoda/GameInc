# Game Inc.

A Game development studio management game made with Unity and C#6, inspired by the likes of Game Dev Story and Game Dev Tycoon.

## Status
Currently in pause while considering a port to Godot 3 since this projet is still in its infancy (this is my first Unity project anyway).

## Implemented Features (WIP)
- Database System loading custom JSON files (supporting comments and multi-line strings) into game objects at runtime. 
The entire game is intended to be described by these files : gaming platforms, random names generation, employee skill sets, game features, events, world news...
The base game module is in the Assets/Core module.
- Script System : statically typed embedded toy language heavily inspired by Rust for its syntax and some of its concepts, with a touch of Typescript and Python. The current lexer is currently very error-prone and difficult to extend. Expression parsing is done with the [Shunting-Yard algorithm] to build an AST that can be evaluated recursively (no bytecode VM yet).
Currently supports primitives (boolean, integer, float, string, Database ID, Date), algebra with parentheses support, constants and variables, function calls, and arrays. All type errors are caught on parsing.
Implemented in about 2500 lines of C#6 in the Assets/Script module.
- Staff Simulation : random employee generation with skills and salary derived from the hiring method
- User Interface : start new game projects, pause or speed up the simulation.
- Market Simulation : dynamic sales of released products (Game or Game Engines only for now) with scoring and hype.
Implemented so as to enable regional-specific market tendencies in the future.
- Building Management : the player can build new rooms, stairs or elevators.

[Shunting-Yard algorithm]: https://www.wikiwand.com/en/Shunting-yard_algorithm
