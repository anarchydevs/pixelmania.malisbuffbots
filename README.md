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

Use the RebuffInfo.json to modify bot rebuffing behavior, example:
{
  "Generic": [
    {
      "Buffs": [ "compatt", "compnano" ] - everyone will buff these two comps to themselves
    }
  ],
  "Trader": [
    {
      "Buffs": [ "mcmo", "tsmo" ] - your trader is going to request these two mochams from your mp
    }
  ]
}

Use the UserRanks.json to modify bot rebuffing behavior, example:
{
  "Moderator": [
    "_InsertNameHere",
    "_InsertNameHere"
  ],
  "Admin": [
    "_InsertNameHere"
  ]
}

 Send a tell to any buffer to execute following commands:
 - "cast <nanoTag1> <nanoTag2> <nanoTag3>" - casts buffs in given order (unranked+)
 - "rebuff" - looks at players ncu and casts all available buffs (unranked+)
 - "buffmacro" - dumps a macro of your current ncu buffs (unranked+)
 - "stand" - makes the bot stand up (moderator+)
 - "sit" - makes the bot stand up (moderator+)
 - "help" - lists all available buffs (unranked+)

 Use the Settings.json to configure bot parameters, example
 {
  "SitKitThreshold": 1000, - if buffers nano drops below this value, they will try to use sit kit
  "PvpFlagCheck": true, - skips buffing flagged people
  "SitKitItemId": 297274, - itemid for automatic sit kit usage
  "IPCChannelId": 255, - if using multiple plugins that take advantage of IPC, make sure that this value is not the same for both plugins
  "InitConnectionDelay": 20.0 - initial delay before bots start processing, you want all of your bots to connect before this value expires
}
 If you log out your bots / kill the process, wait until they fully leave the server before rebooting them, else there might be issues with certain stats not getting registered for the LocalPlayer