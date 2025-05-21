FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RentTrackerClient/RentTrackerClient.csproj", "RentTrackerClient/"]
RUN dotnet restore "RentTrackerClient/RentTrackerClient.csproj"
COPY . .
WORKDIR "/src/RentTrackerClient"
RUN dotnet build "RentTrackerClient.csproj" -c Release -o /app/build
RUN dotnet publish "RentTrackerClient.csproj" -c Release -o /app/publish

FROM node:alpine
WORKDIR /app
COPY --from=build /app/publish/wwwroot .
RUN npm install -g http-server
EXPOSE 80
CMD ["http-server", ".", "-p", "80"]