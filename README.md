# Mission Planner Plugin NamedValueFloatSupport

## How to add to Visual Studio

1. Clone this repo into the `Plugins/` folder directly.

## How to build

Build the entire solution
1. Click `Build` on the top menu strip
2. Build Solution

Or build just the plugin project alone
1. Right click the Project under `Plugins` in the `Solution Explorer`
2. Build
   
   Or push **Ctrl+B**

Result DLLs are by default saved to:
 - Debug build: `<Mission Planner local repo folder>\bin\Debug\net<.Net version number>\NamedValueFloatSupport.dll`
 - Release build: `<Mission Planner local repo folder>\bin\Release\net<.Net version number>\NamedValueFloatSupport.dll`

File names will be whatever is set in Project Properties as the `Assembly name`.

## How to use plugin in Mission Planner

1. Copy NamedValueFloatSupport.dll into the Mission Planner/plugins directory. This is usually found in Program Files (x86)