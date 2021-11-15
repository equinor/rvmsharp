FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim

# additional tools
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-format
RUN dotnet tool install --global dotnet-reportgenerator-globaltool

# restore NuGet packages
COPY ["./CadRevealComposer/CadRevealComposer.csproj", "/src/CadRevealComposer/"]
COPY ["./CadRevealComposer.Exe/CadRevealComposer.Exe.csproj", "/src/CadRevealComposer.Exe/"]
COPY ["./CadRevealComposer.Tests/CadRevealComposer.Tests.csproj", "/src/CadRevealComposer.Tests/"]
COPY ["./HierarchyComposer/HierarchyComposer.csproj", "/src/HierarchyComposer/"]
COPY ["./HierarchyComposer.Tests/HierarchyComposer.Tests.csproj", "/src/HierarchyComposer.Tests/"]
COPY ["./RvmSharp/RvmSharp.csproj", "/src/RvmSharp/"]
COPY ["./RvmSharp.Exe/RvmSharp.Exe.csproj", "/src/RvmSharp.Exe/"]
COPY ["./RvmSharp.Tests/RvmSharp.Tests.csproj", "/src/RvmSharp.Tests/"]
COPY ./rvmsharp.sln /src/
RUN dotnet restore /src/rvmsharp.sln --no-cache --runtime linux-x64

# copy all code
COPY ./CadRevealComposer/. /src/CadRevealComposer/
COPY ./CadRevealComposer.Exe/. /src/CadRevealComposer.Exe/
COPY ./CadRevealComposer.Tests/. /src/CadRevealComposer.Tests/
COPY ./HierarchyComposer/. /src/HierarchyComposer/
COPY ./HierarchyComposer.Tests/. /src/HierarchyComposer.Tests/
COPY ./RvmSharp/. /src/RvmSharp/
COPY ./RvmSharp.Exe/. /src/RvmSharp.Exe/
COPY ./RvmSharp.Tests/. /src/RvmSharp.Tests/
COPY ./tools/. /src/tools/

# build
RUN dotnet build /src/rvmsharp.sln --no-restore --no-incremental --runtime linux-x64 --configuration Release

# test
# UNIT TESTS SHOULD NOT BE REMOVED BASED ON ANY DEVELOPERS PERSONAL PREFERENCE!
RUN dotnet test /src/rvmsharp.sln --no-build --runtime linux-x64 --configuration Release --collect "XPlat Code coverage" --logger:trx --results-directory /testresults ; echo "dotnet test exit code: $?"

# lint - https://github.com/dotnet/format
# LINT CHECKS SHOULD NOT BE REMOVED BASED ON ANY DEVELOPERS PERSONAL PREFERENCE!
RUN dotnet format /src/rvmsharp.sln --no-restore --check --fix-style --fix-analyzers --report /report/DotnetFormatReport.json; echo "dotnet format exit code: $?"

# report generation
RUN reportgenerator -reports:/testresults/**/coverage.cobertura.xml -targetdir:/output -reporttypes:"Cobertura"
