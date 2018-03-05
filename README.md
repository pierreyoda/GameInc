# Game Inc.

A game development studio management game made with Unity and C#6, inspired by games like Game Dev Story and Game Dev Tycoon.

## Status

Currently in pause while considering a port to C#7 and Godot 3 since this projet is still in its infancy (this is my first Unity project anyway).

## Implemented Features (WIP)

- Database System loading custom JSON files (supporting comments and multi-line strings) into game objects at runtime. 
The entire game is intended to be described by these files : gaming platforms, random names generation, employee skill sets, game features, events, world news...
The base game is in the Assets/Resources/Core folder.

- Script System: statically typed embedded toy language heavily inspired by Rust for its syntax and some of its concepts, with some Typescript and Python influence. The current lexer is currently very error-prone and difficult to extend.<br/>
Expression parsing is done with the [Shunting-Yard algorithm] to build an AST that can be evaluated recursively (no bytecode VM yet).<br/><br/>
Currently supports primitives (boolean, integer, float, string, Database ID, Date), algebra with parentheses support, constants and variables, function calls, and arrays. All type errors are caught on parsing.<br/><br/>
Implemented in about 2500 lines of C#6 in the Assets/Script module.

- Staff: random employee generation.

- User Interface: start new game projects, pause or speed up the simulation.

- Market simulation: dynamic sales of released products (game or game engines only for now) with scoring and hype.<br/>
Implemented so as to enable regional-specific market tendencies in the future.

- Building management: the player can build new rooms, stairs or elevators.

## Example of a JSON-defined game event

```
{
    "id": "Company_Engine_CanDevelop",
    "titleEnglish": "Custom Engine development unlocked!",
    "descriptionEnglish": "Having completed {Engine_CanDevelop_MinGames} games, you can now develop your own game engine!",
    "onInit": """
        // comments will not be removed in a JSON multi-string
        let Engine_CanDevelop_MinGames: int = 3; // statically-typed constant
        let Engine_CanDevelop_MinDate: date = 1984/02/01; // game date parsing
        let mut test: float = 0.0; // variable
    """,
    "triggerCondition": """
        // must return a boolean
        // the $ notation is used to access a global game object
        // these global objects are defined in C# code
        ($Company.Projects.CompletedGames.Count() >= Engine_CanDevelop_MinGames)
        &&
        ($World.CurrentDate >= Engine_CanDevelop_MinDate)
    """, 
    "triggerAction": """
        test = 2.0 ^ (3.0 - 1.0); // operations parsing (no type inference yet)
        // global objects can be mutated from script if explicitely allowed
        $Company.Money += test;
        // the id type allows referencing other database items
        let feature_id: id = @Engine.CanDevelop;
        $Company.SetFeature(@Engine.CanDevelop, true);
    """,
    "triggerLimit": """
        // must return an integer
        1 // no ";" : the expression returns its result
    """,
},
```


[Shunting-Yard algorithm]: https://www.wikiwand.com/en/Shunting-yard_algorithm
