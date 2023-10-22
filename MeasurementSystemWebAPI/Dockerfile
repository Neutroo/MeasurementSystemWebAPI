FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MeasurementSystemWebAPI/MeasurementSystemWebAPI.csproj", "MeasurementSystemWebAPI/"]
RUN dotnet restore "MeasurementSystemWebAPI/MeasurementSystemWebAPI.csproj"
COPY . .
WORKDIR "/src/MeasurementSystemWebAPI"
RUN dotnet build "MeasurementSystemWebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MeasurementSystemWebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MeasurementSystemWebAPI.dll"]