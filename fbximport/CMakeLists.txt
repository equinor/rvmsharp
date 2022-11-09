cmake_minimum_required(VERSION 3.13.4)

project (fbximporter LANGUAGES CXX VERSION 0.0.1)

# FBX SDK section
set(FBX_ROOT "$ENV{FBX_ROOT}")
if(FBX_ROOT)
    message(STATUS "FBX found in ${FBX_ROOT}")
else()
    message(FATAL_ERROR "FBX_ROOT is not set in enviroment variables!")
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

set(COMMON_SOURCES
)
set(TARGET_SOURCES
    src/main.cpp
)

include_directories(
    ${fbximporter_SOURCE_DIR}/src)

add_executable(fbximporter ${COMMON_SOURCES} ${TARGET_SOURCES})
add_library(fbximporterlib SHARED ${COMMON_SOURCES} ${TARGET_SOURCES})

target_link_libraries(fbximporter ${FBX_REQUIRED_LIBS_DEPENDENCY})

target_link_libraries(fbximporterlib ${FBX_REQUIRED_LIBS_DEPENDENCY})

# use C++14 standard
set_property(TARGET fbximporter PROPERTY CXX_STANDARD 14)
set_property(TARGET fbximporterlib PROPERTY CXX_STANDARD 14)

target_compile_definitions(fbximporterlib PUBLIC FBXIMPORT_BUILD_AS_DLL)
