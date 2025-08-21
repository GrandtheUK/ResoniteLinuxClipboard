# ResoniteLinuxClipboard

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that implements wayland copy and paste in resonite for Text, Images and other files on Wayland.

This mod should only be used on Wayland Linux. using on X11 or Windows is unsupported.

## Installation
### Quick Install
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Extract [ResoniteLinuxClipboard.zip](https://github.com/GrandtheUK/ResoniteLinuxClipboard/releases/latest/download/ResoniteLinuxClipboard.zip) into your resonite folder. This folder should be at `~/.steam/steam/steamapps/common/Resonite` for a default install. This installs the mod and required rust library
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

### Manual Install
If you want you can download the rust library and the RML mod separately and place them into your resonite install. Place the RML mod into the `rml_mods` folder in your resonite install and the rust library into the `runtimes/linux-x64/native` folder. Without the right version of the rust library this mod will not work correctly if at all, so update both at the same time.