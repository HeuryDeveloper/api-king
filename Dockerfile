# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY King/*.csproj ./King/
RUN dotnet restore "King/King.csproj"
COPY . .
WORKDIR "/src/King"
RUN dotnet publish "King.csproj" -c Release -o /app/publish

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "King.dll"]