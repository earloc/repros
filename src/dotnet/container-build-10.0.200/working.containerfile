ARG DOTNET_VERSION=10.0
ARG DOTNET_SDK_VERSION=${DOTNET_VERSION}

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG BUILD_CONFIGURATION=Release

COPY ./FooProject.csproj  ./FooProject.csproj 
RUN dotnet restore ./FooProject.csproj

COPY ./ /app

RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /bin --no-restore FooProject.csproj

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FooProject.dll"]