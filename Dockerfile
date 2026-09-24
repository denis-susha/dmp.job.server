FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore in a separate layer so it is cached until project files change.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY Job.Server/Job.Server.csproj Job.Server/
COPY DMP.BL/DMP.BL.csproj DMP.BL/
COPY DMP.DataAccess/DMP.DataAccess.csproj DMP.DataAccess/
RUN dotnet restore Job.Server/Job.Server.csproj

COPY . .
RUN dotnet publish Job.Server/Job.Server.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
# dmp.docker sets ASPNETCORE_HTTP_PORTS=80 (the Development profile also binds Kestrel to :80).
EXPOSE 80
ENTRYPOINT ["dotnet", "Job.Server.dll"]
