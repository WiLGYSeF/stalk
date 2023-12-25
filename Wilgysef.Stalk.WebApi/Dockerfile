#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Wilgysef.Stalk.WebApi/Wilgysef.Stalk.WebApi.csproj", "Wilgysef.Stalk.WebApi/"]
COPY ["Infrastructure/Wilgysef.Stalk.EntityFrameworkCore/Wilgysef.Stalk.EntityFrameworkCore.csproj", "Infrastructure/Wilgysef.Stalk.EntityFrameworkCore/"]
COPY ["Wilgysef.Stalk.Core/Wilgysef.Stalk.Core.csproj", "Wilgysef.Stalk.Core/"]
COPY ["Wilgysef.Stalk.Core.Shared/Wilgysef.Stalk.Core.Shared.csproj", "Wilgysef.Stalk.Core.Shared/"]
COPY ["Wilgysef.Stalk.Application/Wilgysef.Stalk.Application.csproj", "Wilgysef.Stalk.Application/"]
COPY ["Wilgysef.Stalk.Application.Contracts/Wilgysef.Stalk.Application.Contracts.csproj", "Wilgysef.Stalk.Application.Contracts/"]

RUN dotnet restore "./Wilgysef.Stalk.WebApi/./Wilgysef.Stalk.WebApi.csproj"
RUN apt install python3 pip3 -y && pip3 install yt-dlp

COPY . .

WORKDIR "/src/Wilgysef.Stalk.WebApi"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Wilgysef.Stalk.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wilgysef.Stalk.WebApi.dll"]
