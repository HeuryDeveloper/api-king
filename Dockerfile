# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY King/King.csproj King/
RUN dotnet restore "King/King.csproj"
COPY . .
WORKDIR "/src/King"
RUN dotnet build "King.csproj" -c Release -o /app/build

# Etapa 2: publish
FROM build AS publish
RUN dotnet publish "King.csproj" -c Release -o /app/publish

# Etapa 3: runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "King.dll"]