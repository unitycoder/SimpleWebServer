# SimpleWebServer
smoll local c# webserver (ps. no security or safety!)

### Features
- Easily start webserver for specific folder from Explorer context menu!
- Customizable port
- Running as admin allows you to host using IP Address (so other computers from your network 192.* can connect)
- F1 = Install Context menu
- F2 = Uninstall Context menu
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
