cmake_minimum_required(VERSION 3.13.4)

project (cfbx LANGUAGES CXX VERSION 0.0.1)

find_package(Catch2 3 REQUIRED)

# FBX SDK section
set(FBX_ROOT "$ENV{FBX_ROOT}")
if(FBX_ROOT)
    message(STATUS "FBX found in ${FBX_ROOT}")
else()
    message(FATAL_ERROR "FBX_ROOT is not set in enviroment variables!")
endif()

if(FBXSDK_VERSION)
    message(STATUS "FBX SDK version set to ${FBXSDK_VERSION}")
else()
    message(FATAL_ERROR "FBXSDK_VERSION is not set!")
endif()

set(FBXSDK_VERSION_THRESH "2020.3.1")
if(${FBXSDK_VERSION} VERSION_GREATER ${FBXSDK_VERSION_THRESH})
else()
    message(FATAL_ERROR "FBXSDK_VERSION has to be newer than ${FBXSDK_VERSION_THRESH}")
endif()

if(NOT ${FBX_ROOT} MATCHES ${FBXSDK_VERSION})
    message(FATAL_ERROR "FBX SDK path does not contain the FBX version " ${FBXSDK_VERSION} )
endif()

include_directories("${FBX_ROOT}/include")
IF (NOT WIN32 AND NOT APPLE)
   # assume we are on Linux
   SET(FBX_SHARED 1)
ENDIF()
include("fbx.cmake")

# http://www.cmake.org/Wiki/CMake_FAQ#How_can_I_build_my_MSVC_application_with_a_static_runtime.3F
if(MSVC AND protobuf_MSVC_STATIC_RUNTIME)
  foreach(flag_var
      CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE
      CMAKE_CXX_FLAGS_MINSIZEREL CMAKE_CXX_FLAGS_RELWITHDEBINFO)
    if(${flag_var} MATCHES "/MD")
      string(REGEX REPLACE "/MD" "/MT" ${flag_var} "${${flag_var}}")
    endif(${flag_var} MATCHES "/MD")
  endforeach()
endif()

set(CMAKE_INCLUDE_CURRENT_DIR TRUE)

set(TARGET_SOURCES
    src/common.h
    src/node.h
    src/node.cpp
    src/main.cpp
    src/mesh.h
    src/mesh.cpp
    src/manager.h
    src/manager.cpp
    src/importer.h
    src/importer.cpp
)
set(TEST_SOURCES
    tests/tests.cpp
)

add_compile_definitions(FBXSDK_VERSION=\"${FBXSDK_VERSION}\")

include_directories(
    ${cfbx_SOURCE_DIR}/src)

add_executable(tests ${TEST_SOURCES} ${TARGET_SOURCES})
target_link_libraries(tests PRIVATE Catch2::Catch2WithMain ${FBX_REQUIRED_LIBS_DEPENDENCY})
set_property(TARGET tests PROPERTY CXX_STANDARD 20)

add_library(cfbx SHARED ${TARGET_SOURCES})
target_link_libraries(cfbx ${FBX_REQUIRED_LIBS_DEPENDENCY})
set_property(TARGET cfbx PROPERTY CXX_STANDARD 20)

target_compile_definitions(cfbx PUBLIC CFBX_BUILD_AS_DLL)
