# Benchmarking Terraria

This repository contains two parts -
1) the docker-compose stack for the terraria server including prometheus and cadvisor
2) the bot runner code, which contains two projects , slightly modified to work
* The base bot code - [Menia](https://github.com/Xwilarg/Meina)
* The client-side library - [TerrariaBot](https://github.com/Xwilarg/TerrariaBot)

## Prerequisites 
- for server-setup stack 
  - docker 
  - docker-compose
- for bot runner 
  - dotnet 3.1 and above 
  - visual studio for running, debugging and building


## How to run the server-setup stack 
- Running the stack - `docker-compose up -d`
- Bringing the stack down - `docker-compose down`


## How to run the bot-runner code 
This is a visual studio project and should be able to load up directly by importing the Menia.sln file. 

Or

Programmatically, `dotnet Meina.dll`
