FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/ ./backend/

RUN dotnet tool install --global dotnet-ef --version 10.0.*
ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet restore backend/src/Worker/Worker.csproj -r linux-x64

RUN dotnet ef migrations bundle \
    --project backend/src/Worker/Worker.csproj \
    --startup-project backend/src/Worker/Worker.csproj \
    --output /app/efbundle \
    --self-contained \
    -r linux-x64

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/efbundle .
RUN chmod +x ./efbundle

ENTRYPOINT ["./efbundle"]

