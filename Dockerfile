FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api/Atlas.Api.csproj apps/api/
RUN dotnet restore apps/api/Atlas.Api.csproj
COPY apps/api/ apps/api/
RUN dotnet tool install --global dotnet-ef --version 10.0.10
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet publish apps/api/Atlas.Api.csproj -c Release -o /app/publish --no-restore
RUN dotnet ef migrations bundle --project apps/api/Atlas.Api.csproj --startup-project apps/api/Atlas.Api.csproj --context AtlasDbContext --configuration Release -o /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000}
COPY --from=build /app/publish ./
COPY --from=build /app/efbundle ./efbundle
COPY apps/api/render-entrypoint.sh ./render-entrypoint.sh
RUN chmod +x ./render-entrypoint.sh ./efbundle
ENTRYPOINT ["/app/render-entrypoint.sh"]
