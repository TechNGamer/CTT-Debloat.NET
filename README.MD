# Ultimate Windows Toolbox
This script is the culmination of many scripts and gists from github with features of my own. I am building this script to be a swiss army knife of Windows tools to help setup and optimize machines.

## My Additions
- Changed the UI from WinForms to using AvaloniaUI.
- Switched mostly away from PowerShell to C#.
  - Some methods still call to PowerShell for help, might change later.
- Added EarTrumpet to the list.

## How to Run
Download it from the Releases section off to the side. Once downloaded, simply run it by double clicking on it.

For complete details check out https://christitus.com/debloat-windows-10-2020/

## Reason for the change
There is a couple of reasons I wanted to do this. The primary reason is that I wanted to see how much I could translate to C#.
The second reason is that I disliked the use of WinForms as it is single-threaded. Whenever interacting with a GUI, the GUI controls
have to be on a single thread. However, the logic can be sent to another. In WinForms, to my understanding, this can be done, just not easily.
Using something like Avalonia (or WPF/UWP) would enable the logic to be sent to background Tasks.

### Why Avalonia
I used Avalonia since I've been trying that out a bit. I like Avalonia since I can make a UI that works on Windows, macOS, and Linux.
I would have used Windows Presentation Foundation, however Avalonia is like WPF but also covers some boiler plate code. Like if you instantiate
a ViewModel, it attempts to find the corresponding View. With WPF, it seems like you have to make it find the Views when instantiate a ViewModel.

Lastly, I am mostly a Linux user, I attempt to send as little time in Windows as possible since it gives me a lot of trouble. That might be from
me trying to use it as a power user. But it might also come down to how Linux does what I want it to do, just better. Since I use both Rider (from JetBrains)
and Avalonia, I can work on the user interface in Linux. Then either reboot into Windows or fire up a Virtual Machine and test it there. If I was to use
WPF, I would have to stay in Windows, which again, I do not like.

## Notices
Since there are some things that I didn't want to dig into during this transformation, and as said above, it still somewhat relies on PowerShell.
Especially when dealing with services. As such, you might see something like below when doing something like the Essential Tweaks:

<img alt="Shows that the program tends to spam PowerShell when doing essential tweaks." src="./RepoAssets/preview.gif" title="PowerShell spam"/>
<sub>This wallpaper is something I made myself using Unity and Wallpaper Engine. Reacts to sounds that are played.</sub>

This program relies on the following:
 - PowerShell (built into Windows)
 - winget

### PowerShell
Some things are still left for PowerShell (as seen/mentioned above), but most of the things have been translated over to C#.
However, controlling services through C# seems like it might be a bit complex. However, PowerShell enabled easier control.
So the program makes basic commands/scripts to execute in PowerShell.

### winget
`winget` is used to install programs with ease. It uses an internal dictionary to figure out what program needs to be installed and what it's
ID is for `winget`.

## Might add later
 * Might add an acrylic effect.
 * Selection box to download and install multiple programs at once instead of individually.
 * Add scroll views to the install column to enable more programs without having to make the window taller.
 * Better handling of service manipulations.
 * Output to a log file on the desktop.
