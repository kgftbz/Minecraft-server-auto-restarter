# Minecraft-server-auto-restarter

Placed in the same folder of `bedrock_server.exe`, launches it with the parameter `..`

Checks server console output expecting the server to send the message `Server heartbeat` every 30 seconds

Once it's been 60 seconds without the heartbeat, it terminates the bedrock_server.exe process and restarts it.

Has the parameter `SaveOutput` to create a log file with server console output,
you shouldn't use this normally as the log file would get huge really quickly, use only for debugging

