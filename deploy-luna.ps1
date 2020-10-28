dotnet run --project C:\Users\harry\Source\neo\seattle\express\src\neoxp\neoxp.csproj -- checkpoint restore .\cp1.neo-express-checkpoint -f
dotnet build
dotnet run --project C:\Users\harry\Source\neo\seattle\express\src\neoxp\neoxp.csproj -- contract deploy ./luna-token/bin/Debug/netstandard2.0/LunaToken.avm owen
dotnet run --project C:\Users\harry\Source\neo\seattle\express\src\neoxp\neoxp.csproj -- contract invoke ./luna-token/deploy.neo-invoke.json owen
dotnet run --project C:\Users\harry\Source\neo\seattle\express\src\neoxp\neoxp.csproj -- transfer ./luna-token/bin/Debug/netstandard2.0/LunaToken.avm 100 owen alice
dotnet run --project C:\Users\harry\Source\neo\seattle\express\src\neoxp\neoxp.csproj -- checkpoint create cp2 --force
