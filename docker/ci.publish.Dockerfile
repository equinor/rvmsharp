# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS dotnet-build
ENV PATH="${PATH}:/root/.dotnet/tools"

# restore NuGet packages
COPY ["./CadRevealComposer/CadRevealComposer.csproj", "/src/CadRevealComposer/"]
COPY ["./CadRevealComposer.Exe/CadRevealComposer.Exe.csproj", "/src/CadRevealComposer.Exe/"]
COPY ["./HierarchyComposer/HierarchyComposer.csproj", "/src/HierarchyComposer/"]
COPY ["./RvmSharp/RvmSharp.csproj", "/src/RvmSharp/"]
RUN dotnet restore /src/CadRevealComposer.Exe/CadRevealComposer.Exe.csproj --no-cache --runtime win10-x64

# build
COPY ./ ./
ARG InformationalVersion
ARG Version
ENV InformationalVersion=$InformationalVersion
ENV Version=$Version
RUN dotnet publish --self-contained false --runtime win10-x64 --no-restore --configuration Release /src/CadRevealComposer.Exe/CadRevealComposer.Exe.csproj  --output /app/CadRevealComposer.Exe/



# Stage 2: Create deploy packages
