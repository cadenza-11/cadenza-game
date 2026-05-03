# BEATDOWN: Unity Game Repo
BEATDOWN is a local-multiplayer beat-em-up rhythm game created using Unity and FMOD.

*This project was initially developed in Spring 2026 as part of the course CS 426 Senior Projects in Computer Science at the University of Nevada, Reno.*

## About
This repo includes both our FMOD-driven multiplayer-rhythm engine (_Cadenza_) as well as contents of the beat-em-up rhythm game (_BEATDOWN_) built atop this architecture.

At a glance, this project features:
* An "FMOD-out" wrapper system which dispatches beat and timeline events from the current audio track to other gameplay systems.
* An "FMOD-in" audio utility system which commands the FMOD runtime to play audio and set audio parameters.
* A multiplayer input system utilizing the most up-to-date practices for shared-screen local multiplayer in Unity.
* Detection and compensation settings for both visual latency and per-player input latency.
* A data-driven UI system utilizing UI Toolkit and new Unity 6.3 features such as custom UI shaders and world-space UI.
* Multiplayer-compatible UI input for both shared screens and player-owned UI hierarchies.
* A local save system utilizing Newtonsoft.JSON for serialization and de-serialization.
* A clear separation of game logic from system or FMOD logic.

## Installing
You may download and play the game from the official [Steam](https://store.steampowered.com/app/4368070/BEATDOWN/) page, or alternatively from this repo's [Github Releases](https://github.com/cadenza-11/cadenza-game/releases) page if you'd like to play through earlier builds.

## Contributing
If you'd like to contribute code to the game, follow the steps below to set up development on your local device.

1. Contact a member of the team to request Github developer access.
2. Download and install the [Unity Hub application](https://unity.com/download).
3. From the Unity Hub, install the **Unity 6000.3.5f2** editor.
4. If using VS Code, download the [Unity extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioToolsForUnity.vstuc) from the VS Code Extension Marketplace.
  > This will also download the C#, C# Dev Kit, and .NET Installer extensions as dependencies.
5. Download and install the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
  > Make sure to download the SDK installer (*not* the runtime installer) for your computer's platform.
  > * On Mac:
  >   * If you are using an Intel Mac, you should be downloading and installing the macOS x64 SDK installer.
  >   * If you are using an ARM-based Mac, you should be downloading and installing the macOS Arm64 SDK installer.
  > * On Windows:
  >   * You should be downloading and installing the Windows x64 SDK
      installer.
6. [Setup an SSH key with Github](https://docs.github.com/en/authentication/connecting-to-github-with-ssh) from your local device.
 > This is necessary in order to gain access to a private Github-hosted Unity package.

## License
Copyright 2026 CadenzaGames. 
All rights reserved.

This repository is public for viewing purposes only.
No usage, modification, distribution, or reproduction of
this code is allowed for any purpose without explicit 
permission.
