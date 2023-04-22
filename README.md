NOTE: BuffsDb.json structure was changed in 21th April 2023 update, if you have custom nanos, make sure to update accordingly

TUTORIAL:   

Use the BuffsDb.json to modify bot casting behavior, example:
{
	"Name": "NCU Nanos", - name of the entry, used in logging information
	"LevelToId": { - level to id map, it will check players level and cast the given nano id, always order highest level to lowest, if we don't care about this just write a single entry with the key being "0"
	"185": 163095,
	"165": 163094,
	"135": 163087,
	"125": 163085,
	"75": 163083,
	"50": 163081,
	"25": 163079
	},
	"Type": "Team", - Player will be invited to team before casting, other option is "Single" for non team buffs
	"TimeOut": 15, - Timeout period, aka how many seconds the bot will attempt to cast this particular nano before moving to the next entry
	"RemoveNanoIdUponCast": 0, - In cases like engi blocker aura, you can specify a custom nano id to be removed from your ncu that would otherwise not allow your bot to cast it again, if we don't care about this leave at 0
	"Tags": [ "ncu" ] - Tags used for commands, aka "cast ncu" would trigger this entry
},

 To use the buffers, type in vicinity "cast <nanoTag1> <nanoTag2> <nanoTag3> .. (multi buffs per line is allowed)
 type "stand" or "sit" to switch their movement states if they are in the wrong initial state
 ORG CHAT: If you want to use org chat for relaying requests, use the Client.Chat.GroupMessageReceived event handler (look in TestPlugin for an example of filtering only org chat messages), and just reroute the commands there
 PRIVATE CHAT: You can use Client.SendPrivateMessage to send messages to people, for command purposes or logging purposes, might want to do it from a single bot to avoid spam

 Use the Settings.json to configure sit kit threshold usage or pvp flag check
 Configure your sit kit item id in the RelevantItems, make sure your character meets the skill requirements to use sit kits if you arent using premium health and nano recharger 
 If you log out your bots / kill the process, wait until they fully leave the server before rebooting them, else there might be issues with certain stats not getting registered for the LocalPlayer
