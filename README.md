# ReadingsBot
Discord Bot for automated posting of lives of the Orthodox saints and eventually other daily readings.
Lives used with permission from OCA(OCA.org).

# Usage
The default prefix for the bot is `%`. This can be changed by calling `%setprefix`. 
If `%` is already used by a bot on the server, you may invoke any command including `setprefix` by mentioning ReadingsBot instead.

Other commands may be seen by running `%help`. In addition, running `%help` with a command name as the argument (`%help [command]`) will give more information about the command.

# Cloning or forking ReadingsBot
The specific data structure and URI to access lives of the saints from the OCA are not currently distributed with this repo.

If you wish to make your own version of ReadingsBot, you will have to modify the existing CacheService to work with whatever JSON feed you wish to use and provide the necessary data structure.

# Dependencies
All the dependencies can be installed using the Nuget Package Manager.
- Discord.NET
- HtmlAgilityPack
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.EnvironmentVariables
- Microsoft.Extensions.DependencyInjection
- MongoDB.Driver
- NodaTime
- NodaTime.Serialization.SystemTextJson
- TimeZoneNames
