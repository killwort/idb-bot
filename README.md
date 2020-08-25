# idb-bot
Idle bot for the Idle Big Devil game.
Runs like any other macro-bot by contorlling input for you.

Currently there's only one mode: arena bot. 
In this mode the bot tries to climb up the arena ladder by selecting opponents with power lower than yours (maximally 90% of your power). That guarantees good win/loss ratio.

1. Install .NET Framework if you don't have one. You'll need at least v.4.6.2.
1. Download arena-bot.7z from releases page
1. Extract package
1. Start the game
1. Skip all the dialogs
1. Go to the arena screen
1. Press battle button
1. Check Fast Battle mode
1. Return to the main arena screen
1. Run IBDTools.exe

If you want to stop running the bot, simply disrupt its process by pressing escape several times to exit to the main game screen. The bot will check if he is at the right page and stop running as soon as it discovers itself being in unknown place.
