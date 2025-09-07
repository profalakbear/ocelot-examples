FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TestAuthSSO.csproj", "."]
RUN dotnet restore "./TestAuthSSO.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TestAuthSSO.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestAuthSSO.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TestAuthSSO.dll"]

