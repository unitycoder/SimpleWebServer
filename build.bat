dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:DebugType=none --self-contained
explorer bin\Release\net7.0\win-x64\publish