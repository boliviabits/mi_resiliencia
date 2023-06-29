#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MiResiliencia/MiResiliencia.csproj", "MiResiliencia/"]
RUN dotnet restore "MiResiliencia/MiResiliencia.csproj"
COPY . .
WORKDIR "/src/MiResiliencia"
RUN dotnet build "MiResiliencia.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MiResiliencia.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update &&\
    apt-get install -y binutils libproj-dev gdal-bin
ENTRYPOINT ["dotnet", "MiResiliencia.dll"]