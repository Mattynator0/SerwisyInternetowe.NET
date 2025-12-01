FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./MainApplication/*.csproj ./MainApplication/
RUN dotnet restore ./MainApplication/MainApplication.csproj

COPY ./MainApplication/. ./MainApplication/
WORKDIR /src/MainApplication

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["dotnet", "MainApplication.dll"]
