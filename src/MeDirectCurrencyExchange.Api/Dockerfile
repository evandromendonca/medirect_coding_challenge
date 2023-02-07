#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MeDirectCurrencyExchange/MeDirectCurrencyExchange.csproj", "MeDirectCurrencyExchange/"]
RUN dotnet restore "MeDirectCurrencyExchange/MeDirectCurrencyExchange.csproj"
COPY . .
WORKDIR "/src/MeDirectCurrencyExchange"
RUN dotnet build "MeDirectCurrencyExchange.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MeDirectCurrencyExchange.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MeDirectCurrencyExchange.dll"]