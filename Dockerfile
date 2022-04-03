#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://*:5000

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RivalCoins.Api/RivalCoins.Api.csproj", "RivalCoins.Api/"]
RUN dotnet restore "RivalCoins.Api/RivalCoins.Api.csproj"
COPY . .
WORKDIR "/src/RivalCoins.Api"
RUN dotnet build "RivalCoins.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RivalCoins.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RivalCoins.Api.dll"]