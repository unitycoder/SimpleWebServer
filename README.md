# SimpleWebServer
[![GitHub license](https://img.shields.io/github/license/unitycoder/SimpleWebServer)](https://github.com/unitycoder/SimpleWebServer/blob/master/LICENSE) [](https://discord.gg/cXT97hU)<a href="https://discord.gg/cXT97hU"><img src="https://img.shields.io/discord/337579253866692608.svg"></a> [![Downloads](https://img.shields.io/github/downloads/unitycoder/simplewebserver/total)](https://github.com/unitycoder/SimpleWebServer/releases/latest/download/SimpleWebServer.zip) [![VirusTotal scan now](https://img.shields.io/static/v1?label=VirusTotal&message=Scan)](https://www.virustotal.com/gui/url/20ad2875b1531a90112d2c8fd2c47a3aa0748ba81b9906c0cee304808c43dc76?nocache=1)

smoll single exe local c# webserver (ps. no security or safety!)

### Features
- Easily start webserver for specific folder from Explorer context menu!
- Customizable port
- Running as admin allows you to host using IP Address (so other computers from your network 192.* can connect)
- F1 = Install Context menu
- F2 = Uninstall Context menu
- F5 = Open Browser (again)
- F12 = Restart App as an Administrator

### USAGE
- Easy method is to use Context menu inside Explorer folder
- SimpleWebServer.exe [root-folder-to-serve] [port]
- SimpleWebServer.exe
- SimpleWebServer.exe [port]
- SimpleWebServer.exe [root-folder-to-serve]

### Example
SimpleWebServer.exe c:\work\website 8080

### Default Settings
[root-folder-to-serve] : application folder (where .exe is located)
[port] : 8080

### Troubleshooting
- Other computer cannot connect? You need to open firewall on the hosting pc (allow inbound TCP 8080)

### Advanced
- To Enable HTTPS: Uncomment those listeners https://github.com/unitycoder/SimpleWebServer/blob/main/Program.cs#L77
- And do the one time setup from this gist:  https://gist.github.com/unitycoder/ec217d20eecc2dfaf8d316acd8c3c5c5

### Images
![image](https://github.com/unitycoder/SimpleWebServer/assets/5438317/9d1a0a31-6752-495f-810a-f0ef8a4ef7f4)
![image](https://github.com/unitycoder/SimpleWebServer/assets/5438317/7eb390ec-aa5f-4c26-9fa6-6fab42ee6bf8)
