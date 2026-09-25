\# FarmNet



A real web browser inside Stardew Valley.



FarmNet is a Stardew Valley SMAPI mod that embeds a Chromium-based web browser directly into the game.



You can browse websites, enter URLs, navigate pages, scroll, interact with web content, watch YouTube, and play browser audio without leaving Stardew Valley.



\## FarmNet 0.1.0



This is the first public release of FarmNet.



More features and improvements are planned.



\## Features



\* Real Chromium-based web browser

\* Address bar for entering URLs

\* Back and forward navigation

\* Page reload

\* Home/favorite button

\* Mouse interaction

\* Keyboard input

\* Mouse wheel scrolling

\* Browser audio

\* YouTube playback

\* Dark browser theme

\* Stardew-inspired parchment theme

\* Theme toggle

\* Browser window can be used while Stardew Valley remains visible behind it



\## Controls



| Input       | Action                              |

| ----------- | ----------------------------------- |

| F2          | Open FarmNet                        |

| Mouse       | Interact with web pages             |

| Keyboard    | Type into web pages and address bar |

| Mouse Wheel | Scroll web pages                    |



\## Requirements



\* Stardew Valley 1.6.15

\* SMAPI 4.5.2

\* Windows x64



FarmNet is currently intended for Windows x64.



\## Installation



1\. Install SMAPI for Stardew Valley.

2\. Download the latest FarmNet release from Nexus Mods.

3\. Extract the `FarmNet` folder into your Stardew Valley `Mods` folder.

4\. Launch Stardew Valley through SMAPI.

5\. Load a save.

6\. Press \*\*F2\*\* to open FarmNet.



\## Building From Source



FarmNet is built using Visual Studio and the .NET 6 Windows target.



\### Project



The project targets:



```text

net6.0-windows

```



and is configured for:



```text

Windows x64

```



\### Dependencies



FarmNet uses the following NuGet packages:



\* Pathoschild.Stardew.ModBuildConfig

\* CefSharp.OffScreen.NETCore

\* System.Drawing.Common

\* NAudio



The exact package versions used by the project are defined in `FarmNet.csproj`.



\### Build



Open `FarmNet.slnx` in Visual Studio.



Select:



```text

Release

Any CPU

```



and build/rebuild the project.



The Stardew mod build configuration automatically handles the release packaging and deployment configuration.



\## Why Does FarmNet Include Chromium Files?



FarmNet embeds a real Chromium-based browser inside Stardew Valley.



The Chromium/CefSharp runtime and its native dependencies are therefore required for FarmNet's browser functionality.



These files are not optional assets. Removing the required Chromium runtime files will prevent the embedded browser from functioning correctly.



\## Source Code



The complete FarmNet source code is available in this repository.



The project is provided so that the implementation can be inspected and the mod can be built from source.



\## Technology



FarmNet is built with:



\* C#

\* .NET 6

\* SMAPI

\* Stardew Valley

\* CefSharp / Chromium

\* MonoGame / Stardew Valley rendering

\* NAudio



\## Current Status



FarmNet is an early release and is actively being developed.



The goal is to continue improving the browser experience and adding functionality while keeping FarmNet lightweight and easy to use.



\## Author



\*\*LennyTheFreak\*\*



FarmNet is an independent Stardew Valley mod project.



