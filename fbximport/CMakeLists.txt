cmake_minimum_required(VERSION 3.13.4)

project (cfbx LANGUAGES CXX VERSION 0.0.1)

list(APPEND CMAKE_MODULE_PATH "${CMAKE_SOURCE_DIR}/cmake")

if (NOT WIN32 AND NOT APPLE)
   # assume we are on Linux
   set(LINUX 1)
endif()

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

include_directories(SYSTEM "${FBX_ROOT}/include")
include(fbx)

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

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")

add_subdirectory(src)
add_subdirectory(tests)
