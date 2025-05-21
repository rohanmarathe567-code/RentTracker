FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RentTrackerBackend/RentTrackerBackend.csproj", "RentTrackerBackend/"]
RUN dotnet restore "RentTrackerBackend/RentTrackerBackend.csproj"
COPY . .
WORKDIR "/src/RentTrackerBackend"
RUN dotnet build "RentTrackerBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RentTrackerBackend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RentTrackerBackend.dll"]