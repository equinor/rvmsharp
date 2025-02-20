# Build the library and tests
FROM ubuntu:jammy AS build
RUN apt update -y; \
    apt upgrade -y; \
    apt install -y build-essential catch2 cmake wget libxml2-dev; \
    apt clean -y
WORKDIR /fbximporter
COPY . .
# Download FBX SDK from autodesk (https://aps.autodesk.com/developer/overview/fbx-sdk). Download link found under `Download SDK for Linux`
RUN wget --no-check-certificate -O fbxsdk.tar.gz https://damassets.autodesk.net/content/dam/autodesk/www/files/fbx202037_fbxsdk_gcc_linux.tar.gz
RUN tar -xzf fbxsdk.tar.gz; \
rm fbxsdk.tar.gz
RUN mkdir -p fbxsdk/2020.3.7
RUN yes yes | ./fbx202037_fbxsdk_linux fbxsdk/2020.3.7; \
rm ./fbx202037_fbxsdk_linux
ENV FBX_ROOT=/fbximporter/fbxsdk/2020.3.7
RUN cmake -B build -S . -D FBXSDK_VERSION=2020.3.7 -D DO_NOT_BUILD_TESTS=1
RUN cmake --build build --config Release

# Copy built binaries to empty image
FROM scratch AS binaries
COPY --from=build /fbximporter/build/bin ./
