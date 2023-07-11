4th July Note: 
- Requests are done now through private messages
- You can now request buffs from any buffer, (ex. can request rrfe from your ma bot, multi buff request is supported), 
- Bots can now automatically rebuff eachother (configurable in RebuffInfo.json), 
- New commands (macrobuffs, rebuff), 
    - Buffmacro - dumps a macro of your current buffs to your character (ex. if you have rrfe and behe it will dump /macro preset /tell buffbot behe rrfe, skips team buffs)
    - Rebuff - checks your ncu and rebuffs you with all available nanos (skips team buffs)
    - Help - Creates a script template of all you buffs and commands
- Generic nano support (like composites, buffers with lowest current queue will buff you these)
- Bots will now skip buffing nanos that they don't have
- Slight db change (will auto migrate to new structure if using old db structure), 
- There is a 20 second initial wait period (configurable in Settings.json) before bots start processing requests (to make sure all bots are connected before requesting)

Read tutorial for more info

TUTORIAL:   

Use the BuffsDb.json to modify bot casting behavior, example:
{
	"Name": "NCU Nanos", - name of the entry, used for logging purposes
	"LevelToId": [ - level to id map, it will check players level and cast the given nano id, always order highest level to lowest, if we don't care about this just write a single entry with the key being "0"
        {
          "Level": 185,
          "Id": 163095
        },
        {
          "Level": 165,
          "Id": 163094
        },
        {
          "Level": 135,
          "Id": 163087
        },
        {
          "Level": 125,
          "Id": 163085
        },
        {
          "Level": 75,
          "Id": 163083
        },
        {
          "Level": 50,
          "Id": 163081
        },
        {
          "Level": 25,
          "Id": 163079
        }
      ],
	"Type": "Team", - Player will be invited to team before casting, other option is "Single" for non team buffs
	"TimeOut": 15, - Timeout period, aka how many seconds the bot will attempt to cast this particular nano before moving to the next entry
	"RemoveNanoIdUponCast": 0, - In cases like engi blocker aura, you can specify a custom nano id to be removed from your ncu that would otherwise not allow your bot to cast it again, if we don't care about this leave at 0
	"Tags": [ "ncu" ] - Tags used for commands, aka "cast ncu" would trigger this entry
},
 Send a tell to any buffer to execute following commands:
 "cast <nanoTag1> <nanoTag2> <nanoTag3>" - casts buffs in given order
 "rebuff" - looks at players ncu and casts all available buffs
 "Buffmacro" - dumps a macro of your current ncu buffs

 type "stand" or "sit" to switch their movement states if they are in the wrong initial state
 ORG CHAT: If you want to use org chat for relaying requests, use the Client.Chat.GroupMessageReceived event handler (look in TestPlugin for an example of filtering only org chat messages), and just reroute the commands there

 Use the Settings.json to configure 
 - Sit kit threshold usage
 - Pvp flag check
 - Sit kit item id
 - IPCChannelId - (0-255) if using multiple plugins that take advantage of IPC, make sure that this value is not the same for both plugins
 - Init connection wait period 
  
 If you log out your bots / kill the process, wait until they fully leave the server before rebooting them, else there might be issues with certain stats not getting registered for the LocalPlayer