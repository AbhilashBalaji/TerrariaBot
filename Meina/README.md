# Terraria bot playing thingy (name wip)
Repo contains two projects , slightly modified to work
* The base bot code - [Menia](https://github.com/Xwilarg/Meina)
* The client-side library - [TerrariaBot](https://github.com/Xwilarg/TerrariaBot)

## How to run it
this is a visual studio project and should be able to load up directly by importing the Menia.sln file (should change xd)


#DEPRICATED 
# Meina
A bot that can play at Terraria

## Starting the bot
After starting the bot, choose if you want to connect it using a direct IP or Steam.<br/>
Provide a password if needed (just fill nothing if there is none)<br/>
And that's all!

## Commands
You can ask the bot to do things by sending chat messages ingame.<br/>
For now the bot will move around using teleportation.<br/>
The command must follow this format:
```
Meina [command] [argument(s)]
```
The available commands are the following:
 - Go to [player name]: Teleport to the location of another player
 - Come here: Teleport to the player's location
 - Go [left/right]: Start running left or right
 - Stop: Stop doing the current action she is doing