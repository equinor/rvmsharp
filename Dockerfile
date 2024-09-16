# Build dotnet project
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
ADD CadRevealComposer CadRevealComposer
ADD CadRevealComposer.Exe CadRevealComposer.Exe
ADD CadRevealFbxProvider CadRevealFbxProvider
ADD CadRevealObjProvider CadRevealObjProvider
ADD CadRevealRvmProvider CadRevealRvmProvider
ADD Commons Commons
ADD HierarchyComposer HierarchyComposer
ADD RvmSharp RvmSharp
RUN dotnet publish "CadRevealComposer.Exe/CadRevealComposer.Exe.csproj" -c $BUILD_CONFIGURATION -o /src/publish

# Setup runtime images
FROM mcr.microsoft.com/dotnet/runtime:8.0-noble
RUN apt update
RUN apt install -y libxml2;
RUN groupadd --gid 1001 echo-group
RUN useradd --uid 1001 --gid 1001 -m echo-user
USER echo-user
WORKDIR /app
COPY --from=build /src/publish .
ENTRYPOINT ["dotnet", "CadRevealComposer.Exe.dll"]
