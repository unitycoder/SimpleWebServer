
# SimpleWebServer
[![GitHub license](https://img.shields.io/github/license/unitycoder/SimpleWebServer)](https://github.com/unitycoder/SimpleWebServer/blob/master/LICENSE) <a href="https://discord.gg/cXT97hU"><img src="https://img.shields.io/discord/337579253866692608.svg"></a> [![Downloads](https://img.shields.io/github/downloads/unitycoder/simplewebserver/total)](https://github.com/unitycoder/SimpleWebServer/releases/latest/download/SimpleWebServer.zip) [![VirusTotal scan now](https://img.shields.io/static/v1?label=VirusTotal&message=Scan)](https://www.virustotal.com/gui/url/20ad2875b1531a90112d2c8fd2c47a3aa0748ba81b9906c0cee304808c43dc76?nocache=1)

Smoll single EXE local C# webserver (ps. no security or safety!).

Designed for local development, testing websites, WebGL, WebXR, WebAR and quickly serving files over a local network.

**Warning:** This application is intended for development and testing on trusted local networks. Do not expose it directly to the internet.

### Features

- Single executable
- Easily start a webserver for a specific folder from the Explorer context menu
- Customizable port
- HTTP and HTTPS support
- Running as administrator allows other devices on your local network to connect using the host PC's IP address
- Optional config.ini file for settings

### Keyboard shortcuts

| Key | Action |
| --- | --- |
| F1 | Install Explorer context menu |
| F2 | Uninstall Explorer context menu |
| F3 | Add executable to user environment PATH |
| F4 | Remove executable from user environment PATH |
| F5 | Open browser |
| F6 | Toggle HTTPS and restart application |
| F12 | Restart application as administrator |
| Enter | Stop server and exit |

### Usage

The easiest method is to use the context menu inside an Explorer folder.

Alternatively, launch the application from the command line:

```
SimpleWebServer.exe [root-folder-to-serve] [port]
SimpleWebServer.exe
SimpleWebServer.exe [port]
SimpleWebServer.exe [root-folder-to-serve]
```

### Example

```
SimpleWebServer.exe c:\work\website 8080
```

### Default settings

- Root folder: Application folder (where the executable is located)
- HTTP port: 8080
- HTTPS port used in the examples below: 4443

### Using config.ini file

Place config.ini in the same folder as the executable.

```ini
ProjectPath=C:\work\website
Port=8080
Silent=True
```

- `ProjectPath` - Path to use as the webserver root.
- `Port` - Webserver port.
- `Silent` - Do not ask whether to use the previously used webserver root folder.

The application may also load and save settings in:

```
%APPDATA%\SimpleWebServer\config.ini
```

Check the console output to see which configuration file is being used.

## Connecting from another device

1. Run SimpleWebServer as administrator.
2. Make sure both devices are connected to the same local network.
3. Allow incoming TCP connections to the webserver port in Windows Firewall.
4. Open the IP address printed by SimpleWebServer in the browser on the other device.

For example:

```
http://192.168.1.100:8080/
```

Use the actual IP address of your hosting PC. You can find it by running:

```cmd
ipconfig
```

Do not use `localhost` on another device, as it refers to that device itself.

## Setting up HTTPS

HTTPS is required by many browser APIs and development tools, including WebXR, WebAR and camera access from mobile browsers.

SimpleWebServer uses Windows HTTP.sys for HTTPS connections. An SSL certificate must be created and bound to the HTTPS port before the server can accept HTTPS connections.

The following instructions use port 4443 and a self-signed certificate.

Run all PowerShell commands as administrator.

### 1. Find your local IP address

```powershell
ipconfig
```

In this example, the hosting PC uses the following IPv4 address:

```
192.168.1.100
```

Replace this address in the following commands with your own IP address.

### 2. Create a self-signed SSL certificate

```powershell
$ip = "192.168.1.100"

$cert = New-SelfSignedCertificate `
    -Subject "CN=SimpleWebServer" `
    -TextExtension @("2.5.29.17={text}DNS=localhost&IPAddress=$ip") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(5)
```

This creates a certificate valid for localhost and the specified local IP address.

Display the certificate thumbprint:

```powershell
$cert.Thumbprint
```

### 3. Bind the certificate to the HTTPS port

Create an application ID and bind the certificate to port 4443:

```powershell
$appId = [guid]::NewGuid().ToString("B")

netsh http add sslcert `
    ipport=0.0.0.0:4443 `
    certhash=$($cert.Thumbprint) `
    appid=$appId `
    certstorename=MY
```

Verify that the certificate binding exists:

```powershell
netsh http show sslcert
```

You should see a binding for:

```
0.0.0.0:4443
```

If a certificate is already bound to this port, inspect the existing binding before replacing it.

If necessary, remove the old binding:

```powershell
netsh http delete sslcert ipport=0.0.0.0:4443
```

Then run the certificate binding command again.

### 4. Trust the certificate on the hosting PC

Export the public certificate:

```powershell
Export-Certificate `
    -Cert $cert `
    -FilePath "$env:USERPROFILE\Desktop\SimpleWebServer.cer"
```

Import it into the trusted root certificate store:

```powershell
Import-Certificate `
    -FilePath "$env:USERPROFILE\Desktop\SimpleWebServer.cer" `
    -CertStoreLocation "Cert:\LocalMachine\Root"
```

### 5. Allow HTTPS connections in Windows Firewall

```powershell
New-NetFirewallRule `
    -DisplayName "SimpleWebServer HTTPS" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 4443 `
    -RemoteAddress LocalSubnet `
    -Action Allow
```

### 6. Enable HTTPS in SimpleWebServer

Run SimpleWebServer as administrator and press F6 to toggle HTTPS.

The application will restart and display its listening addresses.

For example:

```
Listening: https://192.168.1.100:4443/
Listening: https://localhost:4443/
```

Open the HTTPS address in your browser.

### 7. Trust the certificate on your mobile device

When accessing the server from another device, the browser may display a certificate warning because the certificate is self-signed.

For development purposes, you can install the exported SimpleWebServer.cer certificate on your own test devices.

Only transfer the public certificate. Never transfer or share the private key.

#### Android

- Transfer the .cer file to your Android device.
- Open Settings.
- Find Security or Encryption and credentials.
- Select Install a certificate or Install CA certificate.
- Select the exported certificate.

The exact settings depend on the Android version and device manufacturer. Some Android devices or browsers may not accept a self-signed server certificate as a trusted CA.

#### iOS

- Transfer and open the certificate on your device.
- Install the certificate profile if supported.
- Open Settings > General > About > Certificate Trust Settings.
- Enable full trust for the installed certificate if available.

If the device does not accept the certificate, consider using a dedicated local development certificate authority such as mkcert.

Simply bypassing the browser certificate warning may not be sufficient for WebXR, WebAR or other APIs requiring a trusted secure context.

Only install certificates that you created yourself or explicitly trust. Remove development certificates when they are no longer needed.

### Optional: Use mkcert

An alternative way to create locally trusted development certificates is mkcert:

https://github.com/FiloSottile/mkcert

The generated server certificate and private key must be imported into the Windows Local Computer Personal certificate store and bound to the HTTPS port using HTTP.sys.

The mkcert root CA certificate must also be trusted on each device accessing the server.

Never distribute the root CA private key.

## Troubleshooting

### Another computer or mobile device cannot connect

Make sure:

- SimpleWebServer is running as administrator.
- Both devices are connected to the same local network.
- You are using the correct IP address.
- You are using the correct port.
- You are using the correct protocol (HTTP or HTTPS).
- Windows Firewall allows incoming TCP connections.
- Your router does not have AP isolation or client isolation enabled.
- No VPN is interfering with local network connections.

### Allow HTTP connections in Windows Firewall

Open PowerShell as administrator:

```powershell
New-NetFirewallRule `
    -DisplayName "SimpleWebServer HTTP" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 8080 `
    -RemoteAddress LocalSubnet `
    -Action Allow
```

### Allow HTTPS connections in Windows Firewall

```powershell
New-NetFirewallRule `
    -DisplayName "SimpleWebServer HTTPS" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 4443 `
    -RemoteAddress LocalSubnet `
    -Action Allow
```

Change the port number if you are using a custom port.

### Check whether the firewall rules exist

```powershell
Get-NetFirewallRule -DisplayName "SimpleWebServer*" |
    Select-Object DisplayName, Enabled, Direction, Action, Profile
```

### Ping works, but the browser cannot connect

Ping uses ICMP, while the webserver uses TCP. Being able to ping another device does not mean the webserver port is accessible.

First, test whether the server responds locally using its LAN IP address.

For HTTP:

```powershell
curl.exe -v http://192.168.1.100:8080/
```

For HTTPS:

```powershell
curl.exe -vk https://192.168.1.100:4443/
```

The `-k` option ignores certificate validation errors. Use it only for diagnostic purposes.

If the server responds locally but not from another device, the problem is likely related to the firewall or network configuration.

If another Windows PC is available on the same network, test the TCP connection from that PC:

```powershell
Test-NetConnection 192.168.1.100 -Port 8080
```

Look for:

```
TcpTestSucceeded : True
```

### Windows Firewall rule exists, but connections still fail

In some cases, toggling Windows Firewall off and back on may resolve connection problems even when the correct firewall rules already exist.

This has been observed to fix connections from mobile devices to SimpleWebServer.

First, make sure the required firewall rules have been created.

On a trusted local network only, temporarily disable Windows Firewall:

```powershell
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False
```

Test the connection from your mobile device.

Immediately enable Windows Firewall again:

```powershell
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
```

Test the connection again.

Verify that Windows Firewall is enabled:

```powershell
Get-NetFirewallProfile | Select-Object Name, Enabled
```

If the connection continues to work after enabling the firewall, the filtering state may have been refreshed. The exact cause is not confirmed.

**Do not leave Windows Firewall disabled.** This is a temporary diagnostic procedure, not a permanent solution.

### Check Windows network profile

Windows may classify your local network as Public instead of Private.

Check the current network profile:

```powershell
Get-NetConnectionProfile
```

If this is your trusted home network, you can change it to Private:

```powershell
Set-NetConnectionProfile `
    -InterfaceAlias "Ethernet" `
    -NetworkCategory Private
```

Replace Ethernet with your actual network adapter name, such as Wi-Fi.

Do not change public or untrusted networks to Private.

### Check whether the server port is listening

For HTTP:

```cmd
netstat -ano | findstr :8080
```

For HTTPS:

```cmd
netstat -ano | findstr :4443
```

Look for a LISTENING entry.

When using HTTP.sys, the listening process may appear as PID 4 (System). This is normal.

### HTTPS does not work, but HTTP works

Press F6 to temporarily switch back to HTTP.

If HTTP works, check the HTTPS configuration:

- Make sure the SSL certificate is installed.
- Make sure the certificate is bound to the correct port.
- Make sure the certificate has not expired.
- Make sure the certificate includes the current LAN IP address.
- Make sure the HTTPS port is allowed in Windows Firewall.
- Make sure the certificate is trusted on the client device.

Display the current SSL certificate bindings:

```cmd
netsh http show sslcert
```

Display installed certificates:

```powershell
Get-ChildItem Cert:\LocalMachine\My |
    Select-Object Subject, Thumbprint, NotAfter
```

### SEC_E_INVALID_TOKEN error when using curl

Example error:

```
schannel: next InitializeSecurityContext failed:
SEC_E_INVALID_TOKEN (0x80090308)
```

This can happen when attempting to connect using HTTPS to a port that is serving plain HTTP.

Incorrect:

```powershell
curl.exe -vk https://192.168.1.100:8080/
```

Correct, if port 8080 is serving HTTP:

```powershell
curl.exe -v http://192.168.1.100:8080/
```

Always check the listening addresses printed by SimpleWebServer.

### Common browser errors

| Error | Possible cause |
| --- | --- |
| ERR_CONNECTION_TIMED_OUT | Firewall or network filtering |
| ERR_CONNECTION_REFUSED | Server not running or incorrect port |
| ERR_SSL_PROTOCOL_ERROR | Incorrect protocol or SSL configuration |
| ERR_CERT_DATE_INVALID | Expired certificate or incorrect device date |
| ERR_CERT_AUTHORITY_INVALID | Certificate is not trusted |
| ERR_CERT_COMMON_NAME_INVALID | Certificate does not match the server IP or hostname |
| ERR_ADDRESS_UNREACHABLE | Incorrect IP address or network routing issue |

### Local IP address has changed

Your router may assign a different IP address to your PC after a restart or after the DHCP lease expires.

Run:

```cmd
ipconfig
```

Check that you are using the current IP address on your mobile device.

If you are using HTTPS and the certificate contains the old IP address, you may need to generate a new certificate.

To prevent this problem, configure a DHCP reservation for your PC in your router settings.

### Mobile browser still cannot connect

- Temporarily disable mobile data and use Wi-Fi only.
- Disable VPN applications on the mobile device and hosting PC.
- Try another browser.
- Check whether the browser automatically redirects HTTP to HTTPS.
- Check the browser's local network access permissions, if available.
- Make sure the mobile device is not connected to an isolated guest Wi-Fi network.
- Check whether the router has AP isolation enabled.

### Images

![image](https://github.com/unitycoder/SimpleWebServer/assets/5438317/9d1a0a31-6752-495f-810a-f0ef8a4ef7f4)
![image](https://github.com/unitycoder/SimpleWebServer/assets/5438317/7eb390ec-aa5f-4c26-9fa6-6fab42ee6bf8)
