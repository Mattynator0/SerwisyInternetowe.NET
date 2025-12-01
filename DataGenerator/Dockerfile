FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./DataGenerator/*.csproj ./DataGenerator/
RUN dotnet restore ./DataGenerator/DataGenerator.csproj

COPY ./DataGenerator/. ./DataGenerator/
WORKDIR /src/DataGenerator

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5001

ENV ASPNETCORE_URLS=http://0.0.0.0:5001

ENTRYPOINT ["dotnet", "DataGenerator.dll"]
